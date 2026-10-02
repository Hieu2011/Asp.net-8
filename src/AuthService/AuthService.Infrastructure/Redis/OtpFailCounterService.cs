using AuthService.Application.Interfaces;
using AuthService.Application.Options;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace AuthService.Infrastructure.Redis
{
    /// <inheritdoc cref="IOtpLockoutService" />
    /// <remarks>Key thiết kế theo PLAN.md mục 6 — otp_fail_count/otp_locked_until/last_used_otp_step/
    /// password_fail_count/account_locked_until/account_lockout_count, tất cả theo :{userId}.</remarks>
    public class OtpFailCounterService : IOtpLockoutService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly AuthOptions _options;

        public OtpFailCounterService(IConnectionMultiplexer redis, IOptions<AuthOptions> options)
        {
            _redis = redis;
            _options = options.Value;
        }

        private IDatabase Db => _redis.GetDatabase();

        private static string OtpFailKey(Guid userId) => $"otp_fail_count:{userId}";
        private static string OtpLockKey(Guid userId) => $"otp_locked_until:{userId}";
        private static string OtpStepKey(Guid userId) => $"last_used_otp_step:{userId}";
        private static string PasswordFailKey(Guid userId) => $"password_fail_count:{userId}";
        private static string AccountLockKey(Guid userId) => $"account_locked_until:{userId}";
        private static string AccountLockoutCountKey(Guid userId) => $"account_lockout_count:{userId}";

        public async Task<int> IncrementOtpFailAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var key = OtpFailKey(userId);
            var count = await Db.StringIncrementAsync(key);

            // TTL cho counter — nếu không set, key tồn tại vĩnh viễn khi user bỏ dở (sai 1-2 lần
            // rồi không thử lại nữa). Dùng lại đúng cửa sổ OtpLockoutDuration cho "quên" số lần sai
            // cũ nếu quá lâu không có lần thử tiếp theo.
            if (count == 1)
                await Db.KeyExpireAsync(key, _options.OtpLockoutDuration);

            if (count < 3)
                return (int)count;

            // Đạt 3 lần sai — khóa riêng đường OTP + reset counter (mục 17).
            await Db.StringSetAsync(OtpLockKey(userId), "1", _options.OtpLockoutDuration);
            await Db.KeyDeleteAsync(key);
            return 0;
        }

        public Task ResetOtpFailAsync(Guid userId, CancellationToken cancellationToken = default)
            => Db.KeyDeleteAsync(OtpFailKey(userId));

        public async Task<int> GetOtpFailCountAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var value = await Db.StringGetAsync(OtpFailKey(userId));
            return value.HasValue ? (int)value : 0;
        }

        public Task<bool> IsOtpLockedAsync(Guid userId, CancellationToken cancellationToken = default)
            => Db.KeyExistsAsync(OtpLockKey(userId));

        public Task<TimeSpan?> GetOtpLockRemainingAsync(Guid userId, CancellationToken cancellationToken = default)
            => Db.KeyTimeToLiveAsync(OtpLockKey(userId));

        public async Task<bool> TryRecordOtpStepAsync(Guid userId, long timeStep, CancellationToken cancellationToken = default)
        {
            var key = OtpStepKey(userId);
            var previous = await Db.StringGetAsync(key);
            if (previous.HasValue && (long)previous == timeStep)
                return false; // Mục 30 — step trùng lần trước, coi là replay dù mã đúng.

            await Db.StringSetAsync(key, timeStep, TimeSpan.FromSeconds(60));
            return true;
        }

        public async Task<(int FailCount, int LockoutEscalationCount)> IncrementPasswordFailAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var key = PasswordFailKey(userId);
            var failCount = await Db.StringIncrementAsync(key);

            // TTL cho counter — tránh tồn tại vĩnh viễn nếu user bỏ dở giữa chừng (cùng lý do với
            // otp_fail_count ở trên).
            if (failCount == 1)
                await Db.KeyExpireAsync(key, _options.AccountLockoutDuration);

            if (failCount < 3)
            {
                var currentEscalation = await Db.StringGetAsync(AccountLockoutCountKey(userId));
                return ((int)failCount, currentEscalation.HasValue ? (int)currentEscalation : 0);
            }

            // Đạt ngưỡng — khóa cả OTP lẫn Password (mục 18) + tăng escalation count (mục 35).
            await Db.StringSetAsync(AccountLockKey(userId), "1", _options.AccountLockoutDuration);
            await Db.KeyDeleteAsync(key);
            var escalation = await Db.StringIncrementAsync(AccountLockoutCountKey(userId));
            return (0, (int)escalation);
        }

        public Task ResetPasswordFailAsync(Guid userId, CancellationToken cancellationToken = default)
            => Db.KeyDeleteAsync(PasswordFailKey(userId));

        public Task<bool> IsAccountLockedAsync(Guid userId, CancellationToken cancellationToken = default)
            => Db.KeyExistsAsync(AccountLockKey(userId));

        public Task<TimeSpan?> GetAccountLockRemainingAsync(Guid userId, CancellationToken cancellationToken = default)
            => Db.KeyTimeToLiveAsync(AccountLockKey(userId));

        public Task ClearAllLocksAsync(Guid userId, CancellationToken cancellationToken = default)
            => Db.KeyDeleteAsync(new RedisKey[]
            {
                OtpFailKey(userId), OtpLockKey(userId), OtpStepKey(userId),
                PasswordFailKey(userId), AccountLockKey(userId), AccountLockoutCountKey(userId)
            });
    }
}
