using Core.Database;
using Microsoft.Extensions.Logging;
using ML;

namespace BLL
{
    public class BLL_RedisRepository : IBLL_RedisRepository
    {
        private readonly IRedisConnectionService _redis;
        private readonly ILogger<BLL_RedisRepository> _logger;

        public BLL_RedisRepository(IRedisConnectionService redis, ILogger<BLL_RedisRepository> logger)
        {
            _redis = redis;
            _logger = logger;
        }

        #region User Cache
        public async Task<ResultMessage> CacheUserAsync(int userId, object userData, TimeSpan? expiry = null)
        {
            var key = $"user:{userId}";
            expiry ??= TimeSpan.FromMinutes(30);
            
            return await _redis.SetObjectAsync(key, userData, expiry, encrypt: true);
        }

        public async Task<APIResult> GetCachedUserAsync<T>(int userId)
        {
            var key = $"user:{userId}";
            return await _redis.GetObjectAsync<T>(key, encrypted: true);
        }

        public async Task<ResultMessage> RemoveUserCacheAsync(int userId)
        {
            var key = $"user:{userId}";
            return await _redis.KeyDeleteAsync(key);
        }
        #endregion

        #region Rate Limiting
        public async Task<ResultMessage> IncrementRateLimitAsync(string ipAddress)
        {
            var key = $"ratelimit:{ipAddress}";
            return await _redis.IncrementAsync(key, 1, TimeSpan.FromMinutes(1));
        }

        public async Task<APIResult> GetRateLimitAsync(string ipAddress)
        {
            var key = $"ratelimit:{ipAddress}";
            return await _redis.GetCounterAsync(key);
        }

        public async Task<APIResult> IsRateLimitedAsync(string ipAddress, int maxRequests)
        {
            try
            {
                var key = $"ratelimit:{ipAddress}";
                var result = await _redis.GetCounterAsync(key);
                
                if (result.IsError)
                {
                    // Key không tồn tại = chưa bị rate limit
                    return new APIResult(new
                    {
                        IsLimited = false,
                        CurrentCount = 0,
                        MaxRequests = maxRequests,
                        Remaining = maxRequests
                    });
                }

                var data = System.Text.Json.JsonSerializer.Deserialize<dynamic>(result.ResultObject.ToString() ?? "{}");
                long currentCount = data?.Value ?? 0;
                bool isLimited = currentCount >= maxRequests;

                return new APIResult(new
                {
                    IsLimited = isLimited,
                    CurrentCount = currentCount,
                    MaxRequests = maxRequests,
                    Remaining = Math.Max(0, maxRequests - currentCount)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error checking rate limit for IP: {IP}", ipAddress);
                return new APIResult(true, ResultMessage.ErrorTypes.CheckData, "Error checking rate limit", ex.Message);
            }
        }
        #endregion

        #region Session
        public async Task<ResultMessage> SetSessionAsync(string sessionId, object sessionData, TimeSpan? expiry = null)
        {
            var key = $"session:{sessionId}";
            expiry ??= TimeSpan.FromHours(24);
            
            return await _redis.SetObjectAsync(key, sessionData, expiry);
        }

        public async Task<APIResult> GetSessionAsync<T>(string sessionId)
        {
            var key = $"session:{sessionId}";
            return await _redis.GetObjectAsync<T>(key);
        }

        public async Task<ResultMessage> RemoveSessionAsync(string sessionId)
        {
            var key = $"session:{sessionId}";
            return await _redis.KeyDeleteAsync(key);
        }
        #endregion

        #region Generic Cache
        public async Task<ResultMessage> SetCacheAsync<T>(string key, T value, TimeSpan? expiry = null, bool encrypt = false)
        {
            return await _redis.SetObjectAsync(key, value, expiry, encrypt);
        }

        public async Task<APIResult> GetCacheAsync<T>(string key, bool encrypted = false)
        {
            return await _redis.GetObjectAsync<T>(key, encrypted);
        }

        public async Task<ResultMessage> RemoveCacheAsync(string key)
        {
            return await _redis.KeyDeleteAsync(key);
        }

        public async Task<APIResult> GetAllKeysAsync(string pattern = "*")
        {
            return await _redis.KeysScanAsync(pattern);
        }
        #endregion
    }
}