using AuthService.Application.Interfaces;
using StackExchange.Redis;

namespace AuthService.Infrastructure.Redis
{
    /// <inheritdoc cref="IClientRateLimiter" />
    public class ClientRateLimiter : IClientRateLimiter
    {
        private const int MaxAttemptsPerWindow = 30;
        private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

        private readonly IConnectionMultiplexer _redis;

        public ClientRateLimiter(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        private IDatabase Db => _redis.GetDatabase();

        private static string CounterKey(string clientId) => $"client_token_rate:{clientId}";
        private static string AuditKey(string clientId) => $"client_token_audit:{clientId}";

        public async Task<bool> IsAllowedAsync(string clientId, CancellationToken cancellationToken = default)
        {
            var value = await Db.StringGetAsync(CounterKey(clientId));
            return !value.HasValue || (int)value < MaxAttemptsPerWindow;
        }

        public async Task RecordAttemptAsync(string clientId, string? ipAddress, CancellationToken cancellationToken = default)
        {
            var key = CounterKey(clientId);
            var count = await Db.StringIncrementAsync(key);
            if (count == 1)
            {
                await Db.KeyExpireAsync(key, Window);
            }

            // Mục 25 — audit log (IP, thời gian) theo client_id, giữ gọn 100 dòng gần nhất.
            var auditEntry = $"{DateTime.UtcNow:O}|{ipAddress ?? "unknown"}";
            await Db.ListLeftPushAsync(AuditKey(clientId), auditEntry);
            await Db.ListTrimAsync(AuditKey(clientId), 0, 99);
        }
    }
}
