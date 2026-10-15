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
        private const int MaxOtpAttempt = 3;
        private const int MaxPasswordAttempt = 3;
        // KEYS[1]=otp_fail_count, KEYS[2]=otp_locked_until; ARGV[1]=TTL counter (ms), ARGV[2]=số lượt tối đa
        private const string ReserveOtpAttemptScript = """
            if redis.call('EXISTS', KEYS[2]) == 1 then return -1 end
            local c = redis.call('INCR', KEYS[1])
            if c == 1 then redis.call('PEXPIRE', KEYS[1], ARGV[1]) end
            if c > tonumber(ARGV[2]) then redis.call('DECR', KEYS[1]) return -1 end
            return c
            """;
        private const string ReleaseOtpAttemptScript = """
            if tonumber(redis.call('GET', KEYS[1]) or '0') > 0 then redis.call('DECR', KEYS[1]) end
            return 1
            """;
        // Chỉ nhận step lớn hơn step đã dùng — chặn cả replay mã cũ hơn trong cửa sổ ±1
        private const string RecordOtpStepScript = """
            local last = tonumber(redis.call('GET', KEYS[1]) or '-1')
            if tonumber(ARGV[1]) <= last then return 0 end
            redis.call('SET', KEYS[1], ARGV[1], 'EX', 90)
            return 1
            """;
        // Nguyên tử: đúng 1 request chạm ngưỡng mới khóa + leo thang; tài khoản đang khóa thì không đếm nữa
        // (request song song đã lọt qua bước kiểm tra khóa không được đếm thêm vòng mới → không leo thang lên vĩnh viễn trong 1 loạt).
        // KEYS: password_fail_count, account_locked_until, account_lockout_count; ARGV: TTL khóa (ms), cửa sổ leo thang (ms), ngưỡng
        private const string PasswordFailScript = """
            if redis.call('EXISTS', KEYS[2]) == 1 then return {0, 0} end
            local c = redis.call('INCR', KEYS[1])
            if c == 1 then redis.call('PEXPIRE', KEYS[1], ARGV[1]) end
            if c < tonumber(ARGV[3]) then return {c, tonumber(redis.call('GET', KEYS[3]) or '0')} end
            redis.call('SET', KEYS[2], '1', 'PX', ARGV[1])
            redis.call('DEL', KEYS[1])
            local e = redis.call('INCR', KEYS[3])
            redis.call('PEXPIRE', KEYS[3], ARGV[2])
            return {0, e}
            """;

        // Cửa sổ nhớ số lần bị khóa tạm — hết cửa sổ thì không còn leo thang lên khóa vĩnh viễn (mục 35)
        private static readonly TimeSpan LockoutEscalationWindow = TimeSpan.FromHours(24);

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

        public async Task<int?> TryReserveOtpAttemptAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            // Counter tự hết hạn theo OtpLockoutDuration — user bỏ dở sau 1-2 lần sai thì không bị nhớ mãi
            var lngAttempt = (long)await Db.ScriptEvaluateAsync(ReserveOtpAttemptScript,
                [OtpFailKey(userId), OtpLockKey(userId)],
                [(long)_options.OtpLockoutDuration.TotalMilliseconds, MaxOtpAttempt]);
            return lngAttempt < 0 ? null : (int)lngAttempt;
        }

        public Task ReleaseOtpAttemptAsync(Guid userId, CancellationToken cancellationToken = default)
            => Db.ScriptEvaluateAsync(ReleaseOtpAttemptScript, [OtpFailKey(userId)]);

        public async Task RecordOtpFailureAsync(Guid userId, int attemptNumber, CancellationToken cancellationToken = default)
        {
            if (attemptNumber < MaxOtpAttempt)
                return;

            // Đạt 3 lần sai — khóa riêng đường OTP + reset counter (mục 17).
            await Db.StringSetAsync(OtpLockKey(userId), "1", _options.OtpLockoutDuration);
            await Db.KeyDeleteAsync(OtpFailKey(userId));
        }

        public Task ResetOtpFailAsync(Guid userId, CancellationToken cancellationToken = default)
            => Db.KeyDeleteAsync(OtpFailKey(userId));

        public Task<bool> IsOtpLockedAsync(Guid userId, CancellationToken cancellationToken = default)
            => Db.KeyExistsAsync(OtpLockKey(userId));

        public Task<TimeSpan?> GetOtpLockRemainingAsync(Guid userId, CancellationToken cancellationToken = default)
            => Db.KeyTimeToLiveAsync(OtpLockKey(userId));

        public async Task<bool> TryRecordOtpStepAsync(Guid userId, long timeStep, CancellationToken cancellationToken = default)
            => (long)await Db.ScriptEvaluateAsync(RecordOtpStepScript, [OtpStepKey(userId)], [timeStep]) == 1;

        public async Task<(int FailCount, int LockoutEscalationCount)> IncrementPasswordFailAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            // Counter có TTL (user bỏ dở không bị nhớ mãi); đạt 3 → khóa cả OTP lẫn Password (mục 18) + tăng escalation (mục 35)
            var arrResult = (RedisValue[])(await Db.ScriptEvaluateAsync(PasswordFailScript,
                [PasswordFailKey(userId), AccountLockKey(userId), AccountLockoutCountKey(userId)],
                [(long)_options.AccountLockoutDuration.TotalMilliseconds, (long)LockoutEscalationWindow.TotalMilliseconds, MaxPasswordAttempt]))!;
            return ((int)arrResult[0], (int)arrResult[1]);
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
