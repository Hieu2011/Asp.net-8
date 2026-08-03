using ApiCore8.Application.Contracts;
using ApiCore8.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ApiCore8.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RedisTestController : ControllerBase
    {
        private readonly IBLL_RedisRepository _redisRepo;
        private readonly ILogger<RedisTestController> _logger;

        public RedisTestController(IBLL_RedisRepository redisRepo, ILogger<RedisTestController> logger)
        {
            _redisRepo = redisRepo;
            _logger = logger;
        }

        /// <summary>
        /// Test set cache
        /// </summary>
        [HttpPost("SetCache")]
        public async Task<APIResult> SetCache([FromBody] CacheRequest request)
        {
            try
            {
                var expiry = request.ExpiryMinutes.HasValue 
                    ? TimeSpan.FromMinutes(request.ExpiryMinutes.Value)
                    : TimeSpan.FromMinutes(0);

                var result = await _redisRepo.SetCacheAsync(
                    request.Key, 
                    request.Value, 
                    expiry, 
                    request.Encrypt
                );

                if (result.IsError)
                {
                    return new APIResult(true, ResultMessage.ErrorTypes.Insert, result.Message, result.MessageDetail);
                }

                return new APIResult(new
                {
                    Success = true,
                    Message = "Cache set successfully",
                    Key = request.Key,
                    Expiry = expiry.TotalMinutes
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache");
                return new APIResult(true, ResultMessage.ErrorTypes.Insert, "Error setting cache", ex.Message);
            }
        }

        /// <summary>
        /// Test get cache
        /// </summary>
        [HttpGet("GetCache/{key}")]
        public async Task<APIResult> GetCache(string key, [FromQuery] bool encrypted = false)
        {
            try
            {
                return await _redisRepo.GetCacheAsync<object>(key, encrypted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache");
                return new APIResult(true, ResultMessage.ErrorTypes.GetData, "Error getting cache", ex.Message);
            }
        }

        /// <summary>
        /// Test delete cache
        /// </summary>
        [HttpDelete("DeleteCache/{key}")]
        public async Task<APIResult> DeleteCache(string key)
        {
            try
            {
                var result = await _redisRepo.RemoveCacheAsync(key);
                
                if (result.IsError)
                {
                    return new APIResult(true, ResultMessage.ErrorTypes.Delete, result.Message, result.MessageDetail);
                }

                return new APIResult(new
                {
                    Success = true,
                    Message = "Cache deleted successfully",
                    Key = key
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting cache");
                return new APIResult(true, ResultMessage.ErrorTypes.Delete, "Error deleting cache", ex.Message);
            }
        }

        /// <summary>
        /// Test rate limit
        /// </summary>
        [HttpPost("TestRateLimit")]
        public async Task<APIResult> TestRateLimit([FromQuery] string ipAddress = "127.0.0.1")
        {
            try
            {
                // Increment counter
                var incrementResult = await _redisRepo.IncrementRateLimitAsync(ipAddress);
                
                // Check if limited
                var limitResult = await _redisRepo.IsRateLimitedAsync(ipAddress, 10); // Max 10 requests
                
                return limitResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing rate limit");
                return new APIResult(true, ResultMessage.ErrorTypes.Others, "Error testing rate limit", ex.Message);
            }
        }

        /// <summary>
        /// Get all keys
        /// </summary>
        [HttpGet("GetAllKeys")]
        public async Task<APIResult> GetAllKeys([FromQuery] string pattern = "*")
        {
            try
            {
                return await _redisRepo.GetAllKeysAsync(pattern);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all keys");
                return new APIResult(true, ResultMessage.ErrorTypes.SearchData, "Error getting keys", ex.Message);
            }
        }
    }

    public class CacheRequest
    {
        public string Key { get; set; } = string.Empty;
        public object Value { get; set; } = new();
        public int? ExpiryMinutes { get; set; }
        public bool Encrypt { get; set; } = false;
    }
}