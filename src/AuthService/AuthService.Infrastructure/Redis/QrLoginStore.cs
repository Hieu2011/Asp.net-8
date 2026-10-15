using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AuthService.Application.Contracts;
using AuthService.Application.Interfaces;
using AuthService.Application.Options;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace AuthService.Infrastructure.Redis
{
    /// <inheritdoc cref="IQrLoginStore" />
    public sealed class QrLoginStore : IQrLoginStore
    {
        // Lua: chuyển trạng thái nguyên tử — 2 điện thoại bấm duyệt cùng lúc hoặc máy tính poll đúng lúc duyệt vẫn không lệch
        private const string ScanScript = """
            if redis.call('HGET', KEYS[1], 'status') == 'pending' then redis.call('HSET', KEYS[1], 'status', 'scanned') end
            return redis.call('HMGET', KEYS[1], 'status', 'ip', 'device', 'created_at')
            """;
        private const string DecideScript = """
            local s = redis.call('HGET', KEYS[1], 'status')
            if s == 'pending' or s == 'scanned' then redis.call('HSET', KEYS[1], 'status', ARGV[1], 'user_id', ARGV[2]) return 1 end
            return 0
            """;
        private const string PollScript = """
            local h = redis.call('HMGET', KEYS[1], 'secret_hash', 'status', 'user_id')
            if not h[1] then return {'expired'} end
            if h[1] ~= ARGV[1] then return {'expired'} end
            if h[2] == 'approved' then redis.call('DEL', KEYS[1]) return {'approved', h[3]} end
            if h[2] == 'rejected' then redis.call('DEL', KEYS[1]) end
            return {h[2]}
            """;

        private readonly IConnectionMultiplexer _redis;
        private readonly AuthOptions _options;

        public QrLoginStore(IConnectionMultiplexer redis, IOptions<AuthOptions> options)
        {
            _redis = redis;
            _options = options.Value;
        }

        private IDatabase Db => _redis.GetDatabase();

        public async Task<(string ChallengeId, string PollSecret)> CreateAsync(string ipAddress, string? deviceInfo, CancellationToken cancellationToken = default)
        {
            var strChallengeId = GenerateRandomToken(16);
            var strPollSecret = GenerateRandomToken(32);
            var strKey = ChallengeKey(strChallengeId);
            await Db.HashSetAsync(strKey,
            [
                new HashEntry("secret_hash", Hash(strPollSecret)),
                new HashEntry("status", QrLoginStatus.Pending),
                new HashEntry("ip", ipAddress),
                new HashEntry("device", deviceInfo ?? string.Empty),
                new HashEntry("created_at", DateTime.UtcNow.ToString("O"))
            ]);
            await Db.KeyExpireAsync(strKey, _options.QrLoginTtl);
            return (strChallengeId, strPollSecret);
        }

        public async Task<QrChallengeInfo?> ScanAsync(string challengeId, CancellationToken cancellationToken = default)
        {
            var arrValue = (RedisValue[]?)await Db.ScriptEvaluateAsync(ScanScript, [ChallengeKey(challengeId)]);
            if (arrValue == null || arrValue[0].IsNull)
                return null;

            return new QrChallengeInfo
            {
                Status = arrValue[0].ToString(),
                IpAddress = arrValue[1].ToString(),
                DeviceInfo = arrValue[2].IsNullOrEmpty ? null : arrValue[2].ToString(),
                CreatedAt = DateTime.Parse(arrValue[3].ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
            };
        }

        public async Task<bool> DecideAsync(string challengeId, bool isApproved, Guid userId, CancellationToken cancellationToken = default)
        {
            var strStatus = isApproved ? QrLoginStatus.Approved : QrLoginStatus.Rejected;
            return (long)await Db.ScriptEvaluateAsync(DecideScript, [ChallengeKey(challengeId)], [strStatus, userId.ToString()]) == 1;
        }

        public async Task<(string Status, Guid? UserId)> PollAsync(string challengeId, string pollSecret, CancellationToken cancellationToken = default)
        {
            var arrValue = (RedisValue[])(await Db.ScriptEvaluateAsync(PollScript, [ChallengeKey(challengeId)], [Hash(pollSecret ?? string.Empty)]))!;
            var strStatus = arrValue[0].ToString();
            Guid? guidUserId = arrValue.Length > 1 && Guid.TryParse(arrValue[1].ToString(), out var guidParsed) ? guidParsed : null;
            return (strStatus, guidUserId);
        }

        // Key theo hash → lộ dump Redis cũng không dùng lại được challengeId
        private static string ChallengeKey(string strChallengeId) => $"qr_login:{Hash(strChallengeId ?? string.Empty)}";

        private static string GenerateRandomToken(int intByteCount)
            => Convert.ToBase64String(RandomNumberGenerator.GetBytes(intByteCount)).Replace('+', '-').Replace('/', '_').TrimEnd('=');

        private static string Hash(string strValue)
            => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(strValue))).ToLowerInvariant();
    }
}
