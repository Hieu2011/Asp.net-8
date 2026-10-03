using System.Security.Cryptography;
using System.Text;
using AuthService.Application.Interfaces;
using AuthService.Application.Options;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace AuthService.Infrastructure.Redis
{
    /// <inheritdoc cref="IGoogleSetupTokenService" />
    public class GoogleSetupTokenService : IGoogleSetupTokenService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly AuthOptions _options;

        public GoogleSetupTokenService(IConnectionMultiplexer redis, IOptions<AuthOptions> options)
        {
            _redis = redis;
            _options = options.Value;
        }

        private IDatabase Db => _redis.GetDatabase();

        private static string TokenKey(string tokenHash) => $"google_setup_token:{tokenHash}";

        public async Task<string> IssueSetupTokenAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var token = GenerateRandomToken();
            await Db.StringSetAsync(TokenKey(HashToken(token)), userId.ToString(), _options.GoogleSetupTokenTtl);
            return token;
        }

        public async Task<Guid?> ConsumeSetupTokenAsync(string setupToken, CancellationToken cancellationToken = default)
        {
            var key = TokenKey(HashToken(setupToken));
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
