using StackExchange.Redis;
using System.IO.Compression;
using ML;

namespace Core.Database
{
    public interface IRedisConnectionService
    {
        #region Connection Management
        /// <summary>
        /// Lấy Redis database instance
        /// </summary>
        IDatabase GetDatabase(int db = -1);
        
        /// <summary>
        /// Lấy Redis server instance
        /// </summary>
        IServer GetServer();
        
        /// <summary>
        /// Kiểm tra Redis connection status
        /// </summary>
        bool IsConnected { get; }
        #endregion

        #region Encryption/Compression (Utility methods - không cần wrap)
        /// <summary>
        /// Nén và mã hóa string bằng GZip + AES-256 (synchronous)
        /// </summary>
        string CompressAndEncrypt(string text, CompressionLevel compressionLevel = CompressionLevel.Optimal);
        
        /// <summary>
        /// Nén và mã hóa string bằng GZip + AES-256 (asynchronous)
        /// </summary>
        Task<string> CompressAndEncryptAsync(string text, CompressionLevel compressionLevel = CompressionLevel.Optimal);
        
        /// <summary>
        /// Giải mã và giải nén string (synchronous)
        /// </summary>
        string DecompressAndDecrypt(string encryptedText);
        
        /// <summary>
        /// Giải mã và giải nén string (asynchronous)
        /// </summary>
        Task<string> DecompressAndDecryptAsync(string encryptedText);
        #endregion

        #region String Operations
        /// <summary>
        /// Set string value - EXECUTE METHOD
        /// </summary>
        ResultMessage StringSet(string key, string value, TimeSpan? expiry = null);
        
        /// <summary>
        /// Set string value (async) - EXECUTE METHOD
        /// </summary>
        Task<ResultMessage> StringSetAsync(string key, string value, TimeSpan? expiry = null);
        
        /// <summary>
        /// Get string value - QUERY METHOD
        /// </summary>
        APIResult StringGet(string key);
        
        /// <summary>
        /// Get string value (async) - QUERY METHOD
        /// </summary>
        Task<APIResult> StringGetAsync(string key);
        
        /// <summary>
        /// Set object as JSON - EXECUTE METHOD
        /// </summary>
        ResultMessage SetObject<T>(string key, T value, TimeSpan? expiry = null, bool encrypt = false);
        
        /// <summary>
        /// Set object as JSON (async) - EXECUTE METHOD
        /// </summary>
        Task<ResultMessage> SetObjectAsync<T>(string key, T value, TimeSpan? expiry = null, bool encrypt = false);
        
        /// <summary>
        /// Get object from JSON - QUERY METHOD
        /// </summary>
        APIResult GetObject<T>(string key, bool encrypted = false);
        
        /// <summary>
        /// Get object from JSON (async) - QUERY METHOD
        /// </summary>
        Task<APIResult> GetObjectAsync<T>(string key, bool encrypted = false);
        #endregion

        #region Key Management
        /// <summary>
        /// Kiểm tra key có tồn tại không - QUERY METHOD
        /// </summary>
        APIResult KeyExists(string key);
        
        /// <summary>
        /// Kiểm tra key có tồn tại không (async) - QUERY METHOD
        /// </summary>
        Task<APIResult> KeyExistsAsync(string key);
        
        /// <summary>
        /// Xóa key - EXECUTE METHOD
        /// </summary>
        ResultMessage KeyDelete(string key);
        
        /// <summary>
        /// Xóa key (async) - EXECUTE METHOD
        /// </summary>
        Task<ResultMessage> KeyDeleteAsync(string key);
        
        /// <summary>
        /// Xóa nhiều keys - EXECUTE METHOD
        /// </summary>
        ResultMessage KeyDeleteMultiple(string[] keys);
        
        /// <summary>
        /// Xóa nhiều keys (async) - EXECUTE METHOD
        /// </summary>
        Task<ResultMessage> KeyDeleteMultipleAsync(string[] keys);
        
        /// <summary>
        /// Set thời gian hết hạn cho key - EXECUTE METHOD
        /// </summary>
        ResultMessage KeyExpire(string key, TimeSpan expiry);
        
        /// <summary>
        /// Set thời gian hết hạn cho key (async) - EXECUTE METHOD
        /// </summary>
        Task<ResultMessage> KeyExpireAsync(string key, TimeSpan expiry);
        
        /// <summary>
        /// Lấy thời gian còn lại của key - QUERY METHOD
        /// </summary>
        APIResult KeyTimeToLive(string key);
        
        /// <summary>
        /// Lấy thời gian còn lại của key (async) - QUERY METHOD
        /// </summary>
        Task<APIResult> KeyTimeToLiveAsync(string key);
        
        /// <summary>
        /// Tìm keys theo pattern - QUERY METHOD
        /// </summary>
        APIResult KeysScan(string pattern = "*", int pageSize = 250);
        
        /// <summary>
        /// Tìm keys theo pattern (async) - QUERY METHOD
        /// </summary>
        Task<APIResult> KeysScanAsync(string pattern = "*", int pageSize = 250);
        #endregion

        #region Counter Operations
        /// <summary>
        /// Tăng giá trị counter - EXECUTE METHOD
        /// </summary>
        ResultMessage Increment(string key, long value = 1, TimeSpan? expiry = null);
        
        /// <summary>
        /// Tăng giá trị counter (async) - EXECUTE METHOD
        /// </summary>
        Task<ResultMessage> IncrementAsync(string key, long value = 1, TimeSpan? expiry = null);
        
        /// <summary>
        /// Giảm giá trị counter - EXECUTE METHOD
        /// </summary>
        ResultMessage Decrement(string key, long value = 1);
        
        /// <summary>
        /// Giảm giá trị counter (async) - EXECUTE METHOD
        /// </summary>
        Task<ResultMessage> DecrementAsync(string key, long value = 1);
        
        /// <summary>
        /// Lấy giá trị counter - QUERY METHOD
        /// </summary>
        APIResult GetCounter(string key);
        
        /// <summary>
        /// Lấy giá trị counter (async) - QUERY METHOD
        /// </summary>
        Task<APIResult> GetCounterAsync(string key);
        #endregion

        #region Hash Operations
        /// <summary>
        /// Set hash field - EXECUTE METHOD
        /// </summary>
        ResultMessage HashSet(string key, string field, string value);
        
        /// <summary>
        /// Set hash field (async) - EXECUTE METHOD
        /// </summary>
        Task<ResultMessage> HashSetAsync(string key, string field, string value);
        
        /// <summary>
        /// Get hash field - QUERY METHOD
        /// </summary>
        APIResult HashGet(string key, string field);
        
        /// <summary>
        /// Get hash field (async) - QUERY METHOD
        /// </summary>
        Task<APIResult> HashGetAsync(string key, string field);
        
        /// <summary>
        /// Get all hash fields - QUERY METHOD
        /// </summary>
        APIResult HashGetAll(string key);
        
        /// <summary>
        /// Get all hash fields (async) - QUERY METHOD
        /// </summary>
        Task<APIResult> HashGetAllAsync(string key);
        
        /// <summary>
        /// Delete hash field - EXECUTE METHOD
        /// </summary>
        ResultMessage HashDelete(string key, string field);
        
        /// <summary>
        /// Delete hash field (async) - EXECUTE METHOD
        /// </summary>
        Task<ResultMessage> HashDeleteAsync(string key, string field);
        #endregion
    }
}