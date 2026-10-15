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
        private const int MaxOtpFailPerCode = 5;
        private const int MaxOtpFailPerDay = 10;
        private const int MaxSendPerDay = 5;
        private const int SendCooldownSeconds = 60;
        // KEYS: cooldown, daily_send, daily_fail; ARGV: cooldown(s), ngày(ms), max gửi/ngày, max sai/ngày
        private const string ReserveSendScript = """
            if redis.call('EXISTS', KEYS[1]) == 1 then return 0 end
            if tonumber(redis.call('GET', KEYS[3]) or '0') >= tonumber(ARGV[4]) then return 0 end
            local c = redis.call('INCR', KEYS[2])
            if c == 1 then redis.call('PEXPIRE', KEYS[2], ARGV[2]) end
            if c > tonumber(ARGV[3]) then return 0 end
            redis.call('SET', KEYS[1], '1', 'EX', ARGV[1])
            return 1
            """;
        // GET + so sánh hash + đếm sai trong 1 bước — request song song không thử vượt quá giới hạn được.
        // KEYS: otp, fail_per_code, daily_fail; ARGV: hash mã nhập, ngày(ms), max sai/mã, max sai/ngày
        private const string VerifyOtpScript = """
            local stored = redis.call('GET', KEYS[1])
            if not stored then return 0 end
            if tonumber(redis.call('GET', KEYS[3]) or '0') >= tonumber(ARGV[4]) then redis.call('DEL', KEYS[1], KEYS[2]) return 0 end
            if stored == ARGV[1] then redis.call('DEL', KEYS[1], KEYS[2]) return 1 end
            local d = redis.call('INCR', KEYS[3])
            if d == 1 then redis.call('PEXPIRE', KEYS[3], ARGV[2]) end
            if redis.call('INCR', KEYS[2]) >= tonumber(ARGV[3]) then redis.call('DEL', KEYS[1], KEYS[2]) return 0 end
            local t = redis.call('PTTL', KEYS[1])
            if t > 0 then redis.call('PEXPIRE', KEYS[2], t) end
            return 0
            """;
        private const string GetDeleteScript = """
            local v = redis.call('GET', KEYS[1])
            if v then redis.call('DEL', KEYS[1]) end
            return v
            """;

        private static readonly TimeSpan DailyWindow = TimeSpan.FromHours(24);
        private static readonly byte[] OtpHmacInfo = Encoding.UTF8.GetBytes("password-reset-otp");

        private readonly IConnectionMultiplexer _redis;
        private readonly AuthOptions _options;
        private readonly byte[] _arrOtpHmacKey;

        public PasswordResetService(IConnectionMultiplexer redis, IOptions<AuthOptions> options)
        {
            _redis = redis;
            _options = options.Value;
            // Mã 6 số chỉ có 10^6 khả năng — SHA-256 trơn đảo ngược tức thì nếu lộ Redis, nên dùng HMAC khóa server.
            // Khóa dẫn xuất (HKDF) từ khóa sẵn có, tách mục đích bằng info → không thêm secret mới, không dùng lại khóa trực tiếp.
            _arrOtpHmacKey = HKDF.DeriveKey(HashAlgorithmName.SHA256, Encoding.UTF8.GetBytes(_options.TotpEncryptionKey), 32, info: OtpHmacInfo);
        }

        private IDatabase Db => _redis.GetDatabase();

        private static string OtpKey(Guid userId) => $"password_reset_otp:{userId}";
        private static string OtpFailKey(Guid userId) => $"password_reset_otp_fail_count:{userId}";
        private static string CooldownKey(Guid userId) => $"password_reset_otp_cooldown:{userId}";
        private static string DailySendKey(Guid userId) => $"password_reset_daily_send:{userId}";
        private static string DailyFailKey(Guid userId) => $"password_reset_daily_fail:{userId}";
        private static string TokenKey(string tokenHash) => $"password_reset_token:{tokenHash}";

        public async Task<bool> TryReserveSendAsync(Guid userId, CancellationToken cancellationToken = default)
            => (long)await Db.ScriptEvaluateAsync(ReserveSendScript,
                [CooldownKey(userId), DailySendKey(userId), DailyFailKey(userId)],
                [SendCooldownSeconds, (long)DailyWindow.TotalMilliseconds, MaxSendPerDay, MaxOtpFailPerDay]) == 1;

        public async Task<string> GenerateAndStoreOtpAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            await Db.StringSetAsync(OtpKey(userId), HashOtp(otp), _options.PasswordResetOtpTtl);
            await Db.KeyDeleteAsync(OtpFailKey(userId));
            return otp;
        }

        public async Task<bool> VerifyOtpAsync(Guid userId, string otp, CancellationToken cancellationToken = default)
            => (long)await Db.ScriptEvaluateAsync(VerifyOtpScript,
                [OtpKey(userId), OtpFailKey(userId), DailyFailKey(userId)],
                [HashOtp(otp ?? string.Empty), (long)DailyWindow.TotalMilliseconds, MaxOtpFailPerCode, MaxOtpFailPerDay]) == 1;

        public async Task<string> IssueResetTokenAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var token = GenerateRandomToken();
            await Db.StringSetAsync(TokenKey(Hash(token)), userId.ToString(), _options.PasswordResetTokenTtl);
            return token;
        }

        public async Task<Guid?> ConsumeResetTokenAsync(string resetToken, CancellationToken cancellationToken = default)
        {
            var value = (string?)await Db.ScriptEvaluateAsync(GetDeleteScript, [TokenKey(Hash(resetToken))]);
            return Guid.TryParse(value, out var userId) ? userId : null;
        }

        private static string GenerateRandomToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        private string HashOtp(string strOtp)
            => Convert.ToHexString(HMACSHA256.HashData(_arrOtpHmacKey, Encoding.UTF8.GetBytes(strOtp))).ToLowerInvariant();

        private static string Hash(string strValue)
            => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(strValue))).ToLowerInvariant();
    }
}
