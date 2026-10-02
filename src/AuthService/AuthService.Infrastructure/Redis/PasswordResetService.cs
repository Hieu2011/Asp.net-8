using System.Security.Cryptography;
using System.Text;
using AuthService.Application.Interfaces;
using AuthService.Application.Options;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace AuthService.Infrastructure.Redis
{
    /// <inheritdoc cref="IPasswordResetService" />
    public class PasswordResetService : IPasswordResetService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly AuthOptions _options;

        public PasswordResetService(IConnectionMultiplexer redis, IOptions<AuthOptions> options)
        {
            _redis = redis;
            _options = options.Value;
        }

        private IDatabase Db => _redis.GetDatabase();

        private static string OtpKey(Guid userId) => $"password_reset_otp:{userId}";
        private static string OtpFailKey(Guid userId) => $"password_reset_otp_fail_count:{userId}";
        private static string CooldownKey(Guid userId) => $"password_reset_otp_cooldown:{userId}";
        private static string TokenKey(string tokenHash) => $"password_reset_token:{tokenHash}";

        public Task<bool> IsCooldownActiveAsync(Guid userId, CancellationToken cancellationToken = default)
            => Db.KeyExistsAsync(CooldownKey(userId));

        public Task SetCooldownAsync(Guid userId, CancellationToken cancellationToken = default)
            => Db.StringSetAsync(CooldownKey(userId), "1", TimeSpan.FromSeconds(60));

        public async Task<string> GenerateAndStoreOtpAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            await Db.StringSetAsync(OtpKey(userId), otp, _options.PasswordResetOtpTtl);
            await Db.KeyDeleteAsync(OtpFailKey(userId));
            return otp;
        }

        public async Task<bool> VerifyOtpAsync(Guid userId, string otp, CancellationToken cancellationToken = default)
        {
            var stored = await Db.StringGetAsync(OtpKey(userId));
            if (!stored.HasValue)
                return false;

            if (stored == otp)
            {
                await Db.KeyDeleteAsync(OtpKey(userId));
                await Db.KeyDeleteAsync(OtpFailKey(userId));
                return true;
            }

            // Mục 41 — vượt 5 lần sai thì xóa luôn mã cũ, bắt xin mã mới, không cho thử tiếp.
            var failCount = await Db.StringIncrementAsync(OtpFailKey(userId));
            if (failCount >= 5)
            {
                await Db.KeyDeleteAsync(OtpKey(userId));
                await Db.KeyDeleteAsync(OtpFailKey(userId));
            }

            return false;
        }

        public async Task<string> IssueResetTokenAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var token = GenerateRandomToken();
            await Db.StringSetAsync(TokenKey(HashToken(token)), userId.ToString(), _options.PasswordResetTokenTtl);
            return token;
        }

        public async Task<Guid?> ConsumeResetTokenAsync(string resetToken, CancellationToken cancellationToken = default)
        {
            var key = TokenKey(HashToken(resetToken));
            var value = await Db.StringGetAsync(key);
            if (!value.HasValue)
                return null;

            await Db.KeyDeleteAsync(key);
            return Guid.TryParse(value, out var userId) ? userId : null;
        }

        private static string GenerateRandomToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        private static string HashToken(string token)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
