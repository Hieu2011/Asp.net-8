using System.Text.Json;
using AuthService.Application.Contracts;
using AuthService.Application.Interfaces;
using AuthService.Application.Options;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace AuthService.Infrastructure.Redis
{
    /// <inheritdoc cref="IUserProfileCache" />
    public class UserProfileCache : IUserProfileCache
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly AuthOptions _options;

        public UserProfileCache(IConnectionMultiplexer redis, IOptions<AuthOptions> options)
        {
            _redis = redis;
            _options = options.Value;
        }

        private static string Key(Guid userId) => $"user_profile:{userId}";

        public async Task<UserDto?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var json = await _redis.GetDatabase().StringGetAsync(Key(userId));
            return json.HasValue ? JsonSerializer.Deserialize<UserDto>(json!) : null;
        }

        public Task SetAsync(Guid userId, UserDto profile, CancellationToken cancellationToken = default)
            => _redis.GetDatabase().StringSetAsync(Key(userId), JsonSerializer.Serialize(profile), _options.UserProfileCacheTtl);

        public Task RemoveAsync(Guid userId, CancellationToken cancellationToken = default)
            => _redis.GetDatabase().KeyDeleteAsync(Key(userId));
    }
}
