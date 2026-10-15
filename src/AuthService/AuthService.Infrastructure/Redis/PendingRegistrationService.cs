using System.Security.Cryptography;
using System.Text;
using AuthService.Application.Interfaces;
using AuthService.Application.Options;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace AuthService.Infrastructure.Redis
{
    /// <inheritdoc cref="IPendingRegistrationService" />
    public sealed class PendingRegistrationService : IPendingRegistrationService
    {
        private const int MaxOtpFail = 5;
        private const int MaxResend = 5;
        private const string FieldFullName = "full_name";
        private const string FieldEmail = "email";
        private const string FieldPasswordHash = "password_hash";
        private const string FieldOtpHash = "otp_hash";
        private const string FieldFailCount = "fail_count";
        private const string FieldResendCount = "resend_count";
        // Chỉ tăng khi key còn — tránh HINCRBY tạo lại key mồ côi (không TTL) đúng lúc bản chờ vừa hết hạn
        private const string IncrementIfExistsScript =
            "if redis.call('EXISTS', KEYS[1]) == 1 then return redis.call('HINCRBY', KEYS[1], ARGV[1], 1) else return -1 end";

        private readonly IConnectionMultiplexer _redis;
        private readonly AuthOptions _options;

        public PendingRegistrationService(IConnectionMultiplexer redis, IOptions<AuthOptions> options)
        {
            _redis = redis;
            _options = options.Value;
        }

        private IDatabase Db => _redis.GetDatabase();

        public Task<TimeSpan?> GetEmailCooldownAsync(string email, CancellationToken cancellationToken = default)
            => Db.KeyTimeToLiveAsync(EmailCooldownKey(email));

        public async Task<(string RegisterToken, string Otp)> CreateAsync(PendingRegistration registration, CancellationToken cancellationToken = default)
        {
            var strToken = GenerateRandomToken();
            var strOtp = GenerateOtp();
            var strKey = PendingKey(Hash(strToken));

            await Db.HashSetAsync(strKey,
            [
                new HashEntry(FieldFullName, registration.FullName),
                new HashEntry(FieldEmail, registration.Email),
                new HashEntry(FieldPasswordHash, registration.PasswordHash),
                new HashEntry(FieldOtpHash, Hash(strOtp)),
                new HashEntry(FieldFailCount, 0),
                new HashEntry(FieldResendCount, 0)
            ]);
            await Db.KeyExpireAsync(strKey, _options.RegisterOtpTtl);
            await SetCooldownsAsync(strToken, registration.Email);
            return (strToken, strOtp);
        }

        public async Task<PendingRegistration?> GetAsync(string registerToken, CancellationToken cancellationToken = default)
        {
            var arrEntry = await Db.HashGetAllAsync(PendingKey(Hash(registerToken)));
            return arrEntry.Length == 0 ? null : ToRegistration(arrEntry.ToDictionary());
        }

        public async Task<PendingRegistration?> ConsumeAsync(string registerToken, string otp, CancellationToken cancellationToken = default)
        {
            var strKey = PendingKey(Hash(registerToken));
            var dicEntry = (await Db.HashGetAllAsync(strKey)).ToDictionary();
            if (dicEntry.Count == 0)
                return null;

            var arrExpected = Encoding.ASCII.GetBytes(dicEntry[FieldOtpHash].ToString());
            var arrActual = Encoding.ASCII.GetBytes(Hash(otp ?? string.Empty));
            if (CryptographicOperations.FixedTimeEquals(arrExpected, arrActual))
            {
                // KeyDelete trả false nghĩa là request song song đã dùng mất → không cho tạo tài khoản 2 lần
                return await Db.KeyDeleteAsync(strKey) ? ToRegistration(dicEntry) : null;
            }

            if (await IncrementIfExistsAsync(strKey, FieldFailCount) >= MaxOtpFail)
                await Db.KeyDeleteAsync(strKey);
            return null;
        }

        public Task<TimeSpan?> GetResendCooldownAsync(string registerToken, CancellationToken cancellationToken = default)
            => Db.KeyTimeToLiveAsync(ResendCooldownKey(Hash(registerToken)));

        public async Task<string?> RegenerateOtpAsync(string registerToken, CancellationToken cancellationToken = default)
        {
            var strKey = PendingKey(Hash(registerToken));
            var strEmail = (string?)await Db.HashGetAsync(strKey, FieldEmail);
            if (strEmail == null)
                return null;

            var lngResendCount = await IncrementIfExistsAsync(strKey, FieldResendCount);
            if (lngResendCount < 0)
                return null;
            if (lngResendCount > MaxResend)
            {
                await Db.KeyDeleteAsync(strKey);
                return null;
            }

            var strOtp = GenerateOtp();
            await Db.HashSetAsync(strKey, [new HashEntry(FieldOtpHash, Hash(strOtp)), new HashEntry(FieldFailCount, 0)]);
            await Db.KeyExpireAsync(strKey, _options.RegisterOtpTtl);
            await SetCooldownsAsync(registerToken, strEmail);
            return strOtp;
        }

        private async Task<long> IncrementIfExistsAsync(string strKey, string strField)
            => (long)await Db.ScriptEvaluateAsync(IncrementIfExistsScript, [strKey], [strField]);

        private async Task SetCooldownsAsync(string registerToken, string email)
        {
            await Db.StringSetAsync(ResendCooldownKey(Hash(registerToken)), "1", _options.RegisterOtpResendCooldown);
            await Db.StringSetAsync(EmailCooldownKey(email), "1", _options.RegisterOtpResendCooldown);
        }

        private static PendingRegistration ToRegistration(Dictionary<RedisValue, RedisValue> dicEntry) => new()
        {
            FullName = dicEntry[FieldFullName].ToString(),
            Email = dicEntry[FieldEmail].ToString(),
            PasswordHash = dicEntry[FieldPasswordHash].ToString()
        };

        private static string PendingKey(string strTokenHash) => $"register_pending:{strTokenHash}";
        private static string ResendCooldownKey(string strTokenHash) => $"register_resend_cooldown:{strTokenHash}";
        // Hash email để key Redis không chứa thông tin cá nhân dạng rõ
        private static string EmailCooldownKey(string strEmail) => $"register_email_cooldown:{Hash(strEmail)}";

        private static string GenerateOtp() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

        private static string GenerateRandomToken()
            => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace('+', '-').Replace('/', '_').TrimEnd('=');

        private static string Hash(string strValue)
            => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(strValue))).ToLowerInvariant();
    }
}
