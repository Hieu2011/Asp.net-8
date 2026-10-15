using System.Security.Cryptography;
using System.Text;
using AuthService.Application.Interfaces;
using StackExchange.Redis;

namespace AuthService.Infrastructure.Redis
{
    /// <inheritdoc cref="ILoginEmailRateLimiter" />
    public sealed class LoginEmailRateLimiter : ILoginEmailRateLimiter
    {
        private const int MaxAttemptPerWindow = 5;
        // Fixed window nguyên tử: INCR + đặt TTL lượt đầu; vượt ngưỡng trả thời gian còn lại (ms)
        private const string AcquireScript = """
            local c = redis.call('INCR', KEYS[1])
            if c == 1 then redis.call('PEXPIRE', KEYS[1], ARGV[1]) end
            if c > tonumber(ARGV[2]) then return redis.call('PTTL', KEYS[1]) end
            return -1
            """;

        private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

        private readonly IConnectionMultiplexer _redis;

        public LoginEmailRateLimiter(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        public async Task<TimeSpan?> TryAcquireAsync(string email, CancellationToken cancellationToken = default)
        {
            var lngRemainingMs = (long)await _redis.GetDatabase().ScriptEvaluateAsync(AcquireScript,
                [RateKey(email)], [(long)Window.TotalMilliseconds, MaxAttemptPerWindow]);
            return lngRemainingMs < 0 ? null : TimeSpan.FromMilliseconds(Math.Max(lngRemainingMs, 1000));
        }

        // Hash email để key Redis không chứa thông tin cá nhân dạng rõ
        private static string RateKey(string strEmail)
            => $"login_email_rate:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(strEmail))).ToLowerInvariant()}";
    }
}
