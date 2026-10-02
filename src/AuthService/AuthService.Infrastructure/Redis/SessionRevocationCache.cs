using AuthService.Application.Interfaces;
using StackExchange.Redis;

namespace AuthService.Infrastructure.Redis
{
    /// <inheritdoc cref="ISessionRevocationCache" />
    /// <remarks>Redis DÙNG CHUNG với Business API (mục 12) — key `revoked_session:{sessionId}` phải
    /// khớp CHÍNH XÁC tên/format mà `ApiCore8.Api` sẽ đọc ở Bước sau, không tự đổi 1 bên.</remarks>
    public class SessionRevocationCache : ISessionRevocationCache
    {
        private readonly IConnectionMultiplexer _redis;

        public SessionRevocationCache(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        public Task MarkRevokedAsync(Guid sessionId, TimeSpan ttl, CancellationToken cancellationToken = default)
            => _redis.GetDatabase().StringSetAsync($"revoked_session:{sessionId}", "1", ttl);
    }
}
