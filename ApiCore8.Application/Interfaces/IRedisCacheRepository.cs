using ApiCore8.Application.Contracts;
using ApiCore8.Domain.Entities;

namespace ApiCore8.Application.Interfaces
{
    public interface IRedisCacheRepository
    {
        // User cache
        Task<ResultMessage> CacheUserAsync(int userId, object userData, TimeSpan? expiry = null);
        Task<APIResult> GetCachedUserAsync<T>(int userId);
        Task<ResultMessage> RemoveUserCacheAsync(int userId);
        
        // Rate limiting
        Task<ResultMessage> IncrementRateLimitAsync(string ipAddress);
        Task<APIResult> GetRateLimitAsync(string ipAddress);
        Task<APIResult> IsRateLimitedAsync(string ipAddress, int maxRequests);
        
        // Session
        Task<ResultMessage> SetSessionAsync(string sessionId, object sessionData, TimeSpan? expiry = null);
        Task<APIResult> GetSessionAsync<T>(string sessionId);
        Task<ResultMessage> RemoveSessionAsync(string sessionId);
        
        // Generic cache
        Task<ResultMessage> SetCacheAsync<T>(string key, T value, TimeSpan? expiry = null, bool encrypt = false);
        Task<APIResult> GetCacheAsync<T>(string key, bool encrypted = false);
        Task<ResultMessage> RemoveCacheAsync(string key);
        Task<APIResult> GetAllKeysAsync(string pattern = "*");
    }
}