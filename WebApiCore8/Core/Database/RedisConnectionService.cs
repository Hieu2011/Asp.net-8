using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ML;
using Core.Logging; // ✅ Add this

namespace Core.Database
{
    public class RedisConnectionService : IRedisConnectionService, IDisposable
    {
        private readonly Lazy<ConnectionMultiplexer> _lazyConnection;
        private readonly ILogger<RedisConnectionService> _logger;
        private readonly IMongoLoggerService? _mongoLogger; // ✅ Add MongoDB logger
        private readonly string _connectionString;
        private readonly byte[] _aesKey;
        private const string LOG_CATEGORY = "RedisConnectionService"; // ✅ Category constant

        /// <summary>
        /// Constructor - Inject ILogger và IMongoLoggerService
        /// </summary>
        public RedisConnectionService(
            IConfiguration configuration, 
            ILogger<RedisConnectionService> logger,
            IMongoLoggerService? mongoLogger = null) // ✅ Optional injection
        {
            _logger = logger;
            _mongoLogger = mongoLogger; // ✅ Save reference
            
            _connectionString = configuration.GetConnectionString("Redis") 
                ?? "localhost:6380,password=redis123,abortConnect=false";

            var encryptionKey = configuration["Redis:EncryptionKey"] 
                ?? throw new InvalidOperationException("Redis encryption key not configured!");

            using (var sha256 = SHA256.Create())
            {
                _aesKey = sha256.ComputeHash(Encoding.UTF8.GetBytes(encryptionKey));
            }

            // ✅ DUAL LOGGING: ILogger (Serilog) + MongoDB
            _logger.LogInformation("✅ Redis encryption key loaded and hashed (SHA256)");
            _mongoLogger?.LogInformation(LOG_CATEGORY, "Redis encryption key loaded and hashed (SHA256)");

            _lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
            {
                try
                {
                    var connection = ConnectionMultiplexer.Connect(_connectionString);
                    connection.ConnectionFailed += OnConnectionFailed;
                    connection.ConnectionRestored += OnConnectionRestored;
                    connection.ErrorMessage += OnErrorMessage;

                    var endpoints = string.Join(", ", connection.GetEndPoints().Select(ep => ep.ToString()));
                    
                    // ✅ DUAL LOGGING
                    _logger.LogInformation("✅ Redis connected: {Endpoints}", endpoints);
                    _mongoLogger?.LogInformation(LOG_CATEGORY, $"Redis connected: {endpoints}");
                    
                    return connection;
                }
                catch (Exception ex)
                {
                    // ✅ DUAL LOGGING for errors
                    _logger.LogError(ex, "❌ Failed to connect to Redis: {ConnectionString}", _connectionString);
                    _mongoLogger?.LogError(LOG_CATEGORY, $"Failed to connect to Redis: {_connectionString}", ex);
                    throw;
                }
            });
        }

        #region Connection Management
        public IDatabase GetDatabase(int db = -1)
        {
            return _lazyConnection.Value.GetDatabase(db);
        }

        public IServer GetServer()
        {
            var endpoints = _lazyConnection.Value.GetEndPoints();
            return _lazyConnection.Value.GetServer(endpoints.First());
        }

        public bool IsConnected => _lazyConnection.IsValueCreated && _lazyConnection.Value.IsConnected;
        #endregion

        #region Encryption/Compression (Keep as utility - no wrap)
        public string CompressAndEncrypt(string text, CompressionLevel compressionLevel = CompressionLevel.Optimal)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(text);
                byte[] compressedData;
                
                using (var memoryStream = new MemoryStream())
                {
                    using (var gZipStream = new GZipStream(memoryStream, compressionLevel))
                    {
                        gZipStream.Write(buffer, 0, buffer.Length);
                    }
                    compressedData = memoryStream.ToArray();
                }

                using (Aes aes = Aes.Create())
                {
                    aes.Key = _aesKey;
                    aes.GenerateIV();

                    using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                    using (var encryptedStream = new MemoryStream())
                    {
                        encryptedStream.Write(aes.IV, 0, aes.IV.Length);

                        using (var cryptoStream = new CryptoStream(encryptedStream, encryptor, CryptoStreamMode.Write))
                        {
                            cryptoStream.Write(compressedData, 0, compressedData.Length);
                        }

                        return Convert.ToBase64String(encryptedStream.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to compress and encrypt data");
                throw new InvalidOperationException("Encryption failed", ex);
            }
        }

        public async Task<string> CompressAndEncryptAsync(string text, CompressionLevel compressionLevel = CompressionLevel.Optimal)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(text);
                byte[] compressedData;
                
                using (var memoryStream = new MemoryStream())
                {
                    using (var gZipStream = new GZipStream(memoryStream, compressionLevel))
                    {
                        await gZipStream.WriteAsync(buffer, 0, buffer.Length);
                    }
                    compressedData = memoryStream.ToArray();
                }

                using (Aes aes = Aes.Create())
                {
                    aes.Key = _aesKey;
                    aes.GenerateIV();

                    using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                    using (var encryptedStream = new MemoryStream())
                    {
                        await encryptedStream.WriteAsync(aes.IV, 0, aes.IV.Length);

                        using (var cryptoStream = new CryptoStream(encryptedStream, encryptor, CryptoStreamMode.Write))
                        {
                            await cryptoStream.WriteAsync(compressedData, 0, compressedData.Length);
                        }

                        return Convert.ToBase64String(encryptedStream.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to compress and encrypt data (async)");
                throw new InvalidOperationException("Encryption failed", ex);
            }
        }

        public string DecompressAndDecrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return string.Empty;

            try
            {
                byte[] fullCipher = Convert.FromBase64String(encryptedText);

                if (fullCipher.Length < 16)
                {
                    throw new InvalidOperationException($"Encrypted data too short: {fullCipher.Length} bytes");
                }

                byte[] decryptedData;
                using (Aes aes = Aes.Create())
                {
                    aes.Key = _aesKey;

                    byte[] iv = new byte[16];
                    Array.Copy(fullCipher, 0, iv, 0, 16);
                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    using (var decryptedStream = new MemoryStream())
                    {
                        using (var cryptoStream = new CryptoStream(decryptedStream, decryptor, CryptoStreamMode.Write))
                        {
                            cryptoStream.Write(fullCipher, 16, fullCipher.Length - 16);
                        }
                        decryptedData = decryptedStream.ToArray();
                    }
                }

                using (var memoryStream = new MemoryStream(decryptedData))
                using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                using (var resultStream = new MemoryStream())
                {
                    gZipStream.CopyTo(resultStream);
                    return Encoding.UTF8.GetString(resultStream.ToArray());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to decrypt and decompress data");
                throw new InvalidOperationException("Decryption failed", ex);
            }
        }

        public async Task<string> DecompressAndDecryptAsync(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return string.Empty;

            try
            {
                byte[] fullCipher = Convert.FromBase64String(encryptedText);

                if (fullCipher.Length < 16)
                {
                    throw new InvalidOperationException($"Encrypted data too short: {fullCipher.Length} bytes");
                }

                byte[] decryptedData;
                using (Aes aes = Aes.Create())
                {
                    aes.Key = _aesKey;

                    byte[] iv = new byte[16];
                    Array.Copy(fullCipher, 0, iv, 0, 16);
                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    using (var decryptedStream = new MemoryStream())
                    {
                        using (var cryptoStream = new CryptoStream(decryptedStream, decryptor, CryptoStreamMode.Write))
                        {
                            await cryptoStream.WriteAsync(fullCipher, 16, fullCipher.Length - 16);
                        }
                        decryptedData = decryptedStream.ToArray();
                    }
                }

                using (var memoryStream = new MemoryStream(decryptedData))
                using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                using (var resultStream = new MemoryStream())
                {
                    await gZipStream.CopyToAsync(resultStream);
                    return Encoding.UTF8.GetString(resultStream.ToArray());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to decrypt and decompress data (async)");
                throw new InvalidOperationException("Decryption failed", ex);
            }
        }
        #endregion

        #region String Operations
        public ResultMessage StringSet(string key, string value, TimeSpan? expiry = null)
        {
            try
            {
                var db = GetDatabase();
                bool success = db.StringSet(key, value, expiry);
                
                if (success)
                {
                    _logger.LogDebug("✅ Set key: {Key} with expiry: {Expiry}", key, expiry?.TotalSeconds);
                    return new ResultMessage(false, ResultMessage.ErrorTypes.No_Error, "Key set successfully");
                }
                
                return new ResultMessage(true, ResultMessage.ErrorTypes.Update, "Failed to set key", $"Redis returned false for key: {key}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to set key: {Key}", key);
                return new ResultMessage(true, ResultMessage.ErrorTypes.Update, "Error setting key", ex.Message);
            }
        }

        public async Task<ResultMessage> StringSetAsync(string key, string value, TimeSpan? expiry = null)
        {
            try
            {
                var db = GetDatabase();
                bool success = await db.StringSetAsync(key, value, expiry);
                
                if (success)
                {
                    _logger.LogDebug("✅ Set key (async): {Key} with expiry: {Expiry}", key, expiry?.TotalSeconds);
                    return new ResultMessage(false, ResultMessage.ErrorTypes.No_Error, "Key set successfully");
                }
                
                return new ResultMessage(true, ResultMessage.ErrorTypes.Update, "Failed to set key", $"Redis returned false for key: {key}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to set key (async): {Key}", key);
                return new ResultMessage(true, ResultMessage.ErrorTypes.Update, "Error setting key", ex.Message);
            }
        }

        public APIResult StringGet(string key)
        {
            try
            {
                var db = GetDatabase();
                var value = db.StringGet(key);
                
                if (value.HasValue)
                {
                    return new APIResult(value.ToString());
                }
                
                return new APIResult(true, ResultMessage.ErrorTypes.GetData, "Key not found", $"Key '{key}' does not exist in Redis");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to get key: {Key}", key);
                return new APIResult(true, ResultMessage.ErrorTypes.GetData, "Error getting key", ex.Message);
            }
        }

        public async Task<APIResult> StringGetAsync(string key)
        {
            try
            {
                var db = GetDatabase();
                var value = await db.StringGetAsync(key);
                
                if (value.HasValue)
                {
                    return new APIResult(value.ToString());
                }
                
                return new APIResult(true, ResultMessage.ErrorTypes.GetData, "Key not found", $"Key '{key}' does not exist in Redis");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to get key (async): {Key}", key);
                return new APIResult(true, ResultMessage.ErrorTypes.GetData, "Error getting key", ex.Message);
            }
        }

        public ResultMessage SetObject<T>(string key, T value, TimeSpan? expiry = null, bool encrypt = false)
        {
            try
            {
                var json = JsonSerializer.Serialize(value);
                var data = encrypt ? CompressAndEncrypt(json) : json;
                return StringSet(key, data, expiry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to set object: {Key}", key);
                return new ResultMessage(true, ResultMessage.ErrorTypes.Update, "Error setting object", ex.Message);
            }
        }

        public async Task<ResultMessage> SetObjectAsync<T>(string key, T value, TimeSpan? expiry = null, bool encrypt = false)
        {
            try
            {
                var json = JsonSerializer.Serialize(value);
                var data = encrypt ? await CompressAndEncryptAsync(json) : json;
                return await StringSetAsync(key, data, expiry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to set object (async): {Key}", key);
                return new ResultMessage(true, ResultMessage.ErrorTypes.Update, "Error setting object", ex.Message);
            }
        }

        public APIResult GetObject<T>(string key, bool encrypted = false)
        {
            try
            {
                var result = StringGet(key);
                if (result.IsError)
                    return result;

                var data = result.ResultObject?.ToString();
                if (string.IsNullOrEmpty(data))
                    return new APIResult(true, ResultMessage.ErrorTypes.GetData, "Empty data", "Retrieved data is null or empty");

                var json = encrypted ? DecompressAndDecrypt(data) : data;
                var obj = JsonSerializer.Deserialize<T>(json);
                
                return new APIResult(obj);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to get object: {Key}", key);
                return new APIResult(true, ResultMessage.ErrorTypes.GetData, "Error getting object", ex.Message);
            }
        }

        public async Task<APIResult> GetObjectAsync<T>(string key, bool encrypted = false)
        {
            try
            {
                var result = await StringGetAsync(key);
                if (result.IsError)
                    return result;

                var data = result.ResultObject?.ToString();
                if (string.IsNullOrEmpty(data))
                    return new APIResult(true, ResultMessage.ErrorTypes.GetData, "Empty data", "Retrieved data is null or empty");

                var json = encrypted ? await DecompressAndDecryptAsync(data) : data;
                var obj = JsonSerializer.Deserialize<T>(json);
                
                return new APIResult(obj);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to get object (async): {Key}", key);
                return new APIResult(true, ResultMessage.ErrorTypes.GetData, "Error getting object", ex.Message);
            }
        }
        #endregion

        #region Key Management
        public APIResult KeyExists(string key)
        {
            try
            {
                var db = GetDatabase();
                bool exists = db.KeyExists(key);
                
                return new APIResult(exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to check key exists: {Key}", key);
                return new APIResult(true, ResultMessage.ErrorTypes.CheckData, "Error checking key existence", ex.Message);
            }
        }

        public async Task<APIResult> KeyExistsAsync(string key)
        {
            try
            {
                var db = GetDatabase();
                bool exists = await db.KeyExistsAsync(key);
                
                return new APIResult(exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to check key exists (async): {Key}", key);
                return new APIResult(true, ResultMessage.ErrorTypes.CheckData, "Error checking key existence", ex.Message);
            }
        }

        public ResultMessage KeyDelete(string key)
        {
            try
            {
                var db = GetDatabase();
                bool deleted = db.KeyDelete(key);
                
                if (deleted)
                {
                    _logger.LogDebug("✅ Deleted key: {Key}", key);
                    return new ResultMessage(false, ResultMessage.ErrorTypes.No_Error, "Key deleted successfully");
                }
                
                return new ResultMessage(true, ResultMessage.ErrorTypes.Delete, "Key not found", $"Key '{key}' does not exist");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to delete key: {Key}", key);
                return new ResultMessage(true, ResultMessage.ErrorTypes.Delete, "Error deleting key", ex.Message);
            }
        }

        public async Task<ResultMessage> KeyDeleteAsync(string key)
        {
            try
            {
                var db = GetDatabase();
                bool deleted = await db.KeyDeleteAsync(key);
                
                if (deleted)
                {
                    _logger.LogDebug("✅ Deleted key (async): {Key}", key);
                    return new ResultMessage(false, ResultMessage.ErrorTypes.No_Error, "Key deleted successfully");
                }
                
                return new ResultMessage(true, ResultMessage.ErrorTypes.Delete, "Key not found", $"Key '{key}' does not exist");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to delete key (async): {Key}", key);
                return new ResultMessage(true, ResultMessage.ErrorTypes.Delete, "Error deleting key", ex.Message);
            }
        }

        public ResultMessage KeyDeleteMultiple(string[] keys)
        {
            try
            {
                var db = GetDatabase();
                var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
                long deletedCount = db.KeyDelete(redisKeys);
                
                _logger.LogDebug("✅ Deleted {Count} keys out of {Total}", deletedCount, keys.Length);
                return new ResultMessage(false, ResultMessage.ErrorTypes.No_Error, 
                    $"Deleted {deletedCount}/{keys.Length} keys", 
                    $"Successfully deleted {deletedCount} keys");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to delete multiple keys");
                return new ResultMessage(true, ResultMessage.ErrorTypes.Delete, "Error deleting multiple keys", ex.Message);
            }
        }

        public async Task<ResultMessage> KeyDeleteMultipleAsync(string[] keys)
        {
            try
            {
                var db = GetDatabase();
                var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
                long deletedCount = await db.KeyDeleteAsync(redisKeys);
                
                _logger.LogDebug("✅ Deleted {Count} keys out of {Total} (async)", deletedCount, keys.Length);
                return new ResultMessage(false, ResultMessage.ErrorTypes.No_Error, 
                    $"Deleted {deletedCount}/{keys.Length} keys", 
                    $"Successfully deleted {deletedCount} keys");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to delete multiple keys (async)");
                return new ResultMessage(true, ResultMessage.ErrorTypes.Delete, "Error deleting multiple keys", ex.Message);
            }
        }

        public ResultMessage KeyExpire(string key, TimeSpan expiry)
        {
            try
            {
                var db = GetDatabase();
                bool success = db.KeyExpire(key, expiry);
                
                if (success)
                {
                    _logger.LogDebug("✅ Set expiry for key: {Key} = {Seconds}s", key, expiry.TotalSeconds);
                    return new ResultMessage(false, ResultMessage.ErrorTypes.No_Error, "Expiry set successfully");
                }
                
                return new ResultMessage(true, ResultMessage.ErrorTypes.Update, "Key not found", $"Key '{key}' does not exist");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to set expiry for key: {Key}", key);
                return new ResultMessage(true, ResultMessage.ErrorTypes.Update, "Error setting expiry", ex.Message);
            }
        }

        public async Task<ResultMessage> KeyExpireAsync(string key, TimeSpan expiry)
        {
            try
            {
                var db = GetDatabase();
                bool success = await db.KeyExpireAsync(key, expiry);
                
                if (success)
                {
                    _logger.LogDebug("✅ Set expiry for key (async): {Key} = {Seconds}s", key, expiry.TotalSeconds);
                    return new ResultMessage(false, ResultMessage.ErrorTypes.No_Error, "Expiry set successfully");
                }
                
                return new ResultMessage(true, ResultMessage.ErrorTypes.Update, "Key not found", $"Key '{key}' does not exist");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to set expiry for key (async): {Key}", key);
                return new ResultMessage(true, ResultMessage.ErrorTypes.Update, "Error setting expiry", ex.Message);
            }
        }

        public APIResult KeyTimeToLive(string key)
        {
            try
            {
                var db = GetDatabase();
                var ttl = db.KeyTimeToLive(key);
                
                return new APIResult(new
                {
                    Key = key,
                    TTL = ttl,
                    Seconds = ttl?.TotalSeconds,
                    HasExpiry = ttl.HasValue
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to get TTL for key: {Key}", key);
                return new APIResult(true, ResultMessage.ErrorTypes.GetData, "Error getting TTL", ex.Message);
            }
        }

        public async Task<APIResult> KeyTimeToLiveAsync(string key)
        {
            try
            {
                var db = GetDatabase();
                var ttl = await db.KeyTimeToLiveAsync(key);
                
                return new APIResult(new
                {
                    Key = key,
                    TTL = ttl,
                    Seconds = ttl?.TotalSeconds,
                    HasExpiry = ttl.HasValue
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to get TTL for key (async): {Key}", key);
                return new APIResult(true, ResultMessage.ErrorTypes.GetData, "Error getting TTL", ex.Message);
            }
        }

        public APIResult KeysScan(string pattern = "*", int pageSize = 250)
        {
            try
            {
                var server = GetServer();
                var keys = server.Keys(pattern: pattern, pageSize: pageSize)
                    .Select(k => k.ToString())
                    .ToList();
                
                return new APIResult(new
                {
                    Pattern = pattern,
                    Count = keys.Count,
                    Keys = keys
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to scan keys with pattern: {Pattern}", pattern);
                return new APIResult(true, ResultMessage.ErrorTypes.SearchData, "Error scanning keys", ex.Message);
            }
        }

        public async Task<APIResult> KeysScanAsync(string pattern = "*", int pageSize = 250)
        {
            return await Task.Run(() => KeysScan(pattern, pageSize));
        }
        #endregion

        #region Counter Operations
        public ResultMessage Increment(string key, long value = 1, TimeSpan? expiry = null)
        {
            try
            {
                var db = GetDatabase();
                long result = db.StringIncrement(key, value);
                
                // Set expiry nếu là lần đầu (result == value)
                if (expiry.HasValue && result == value)
                {
                    db.KeyExpire(key, expiry.Value);
                }
                
                _logger.LogDebug("✅ Incremented key: {Key} by {Value}, new value: {Result}", key, value, result);
                return new ResultMessage(false, ResultMessage.ErrorTypes.No_Error, 
                    "Counter incremented", 
                    $"New value: {result}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to increment key: {Key}", key);
                return new ResultMessage(true, ResultMessage.ErrorTypes.Update, "Error incrementing counter", ex.Message);
            }
        }

        public async Task<ResultMessage> IncrementAsync(string key, long value = 1, TimeSpan? expiry = null)
        {
            try
            {
                var db = GetDatabase();
                long result = await db.StringIncrementAsync(key, value);
                
                // Set expiry nếu là lần đầu
                if (expiry.HasValue && result == value)
                {
                    await db.KeyExpireAsync(key, expiry.Value);
                }
                
                _logger.LogDebug("✅ Incremented key (async): {Key} by {Value}, new value: {Result}", key, value, result);
                return new ResultMessage(false, ResultMessage.ErrorTypes.No_Error, 
                    "Counter incremented", 
                    $"New value: {result}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to increment key (async): {Key}", key);
                return new ResultMessage(true, ResultMessage.ErrorTypes.Update, "Error incrementing counter", ex.Message);
            }
        }

        public ResultMessage Decrement(string key, long value = 1)
        {
            try
            {
                var db = GetDatabase();
                long result = db.StringDecrement(key, value);
                
                _logger.LogDebug("✅ Decremented key: {Key} by {Value}, new value: {Result}", key, value, result);
                return new ResultMessage(false, ResultMessage.ErrorTypes.No_Error, 
                    "Counter decremented", 
                    $"New value: {result}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to decrement key: {Key}", key);
                return new ResultMessage(true, ResultMessage.ErrorTypes.Update, "Error decrementing counter", ex.Message);
            }
        }

        public async Task<ResultMessage> DecrementAsync(string key, long value = 1)
        {
            try
            {
                var db = GetDatabase();
                long result = await db.StringDecrementAsync(key, value);
                
                _logger.LogDebug("✅ Decremented key (async): {Key} by {Value}, new value: {Result}", key, value, result);
                return new ResultMessage(false, ResultMessage.ErrorTypes.No_Error, 
                    "Counter decremented", 
                    $"New value: {result}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to decrement key (async): {Key}", key);
                return new ResultMessage(true, ResultMessage.ErrorTypes.Update, "Error decrementing counter", ex.Message);
            }
        }

        public APIResult GetCounter(string key)
        {
            try
            {
                var db = GetDatabase();
                var value = db.StringGet(key);
                
                if (value.HasValue && long.TryParse(value, out long counter))
                {
                    return new APIResult(new
                    {
                        Key = key,
                        Value = counter
                    });
                }
                
                return new APIResult(true, ResultMessage.ErrorTypes.GetData, "Counter not found or invalid", $"Key '{key}' is not a valid counter");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to get counter: {Key}", key);
                return new APIResult(true, ResultMessage.ErrorTypes.GetData, "Error getting counter", ex.Message);
            }
        }

        public async Task<APIResult> GetCounterAsync(string key)
        {
            try
            {
                var db = GetDatabase();
                var value = await db.StringGetAsync(key);
                
                if (value.HasValue && long.TryParse(value, out long counter))
                {
                    return new APIResult(new
                    {
                        Key = key,
                        Value = counter
                    });
                }
                
                return new APIResult(true, ResultMessage.ErrorTypes.GetData, "Counter not found or invalid", $"Key '{key}' is not a valid counter");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to get counter (async): {Key}", key);
                return new APIResult(true, ResultMessage.ErrorTypes.GetData, "Error getting counter", ex.Message);
            }
        }
        #endregion

        #region Hash Operations
        public ResultMessage HashSet(string key, string field, string value)
        {
            try
            {
                var db = GetDatabase();
                bool success = db.HashSet(key, field, value);
                
                _logger.LogDebug("✅ Set hash field: {Key}.{Field}", key, field);
                return new ResultMessage(false, ResultMessage.ErrorTypes.No_Error, "Hash field set successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to set hash field: {Key}.{Field}", key, field);
                return new ResultMessage(true, ResultMessage.ErrorTypes.Update, "Error setting hash field", ex.Message);
            }
        }

        public async Task<ResultMessage> HashSetAsync(string key, string field, string value)
        {
            try
            {
                var db = GetDatabase();
                bool success = await db.HashSetAsync(key, field, value);
                
                _logger.LogDebug("✅ Set hash field (async): {Key}.{Field}", key, field);
                return new ResultMessage(false, ResultMessage.ErrorTypes.No_Error, "Hash field set successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to set hash field (async): {Key}.{Field}", key, field);
                return new ResultMessage(true, ResultMessage.ErrorTypes.Update, "Error setting hash field", ex.Message);
            }
        }

        public APIResult HashGet(string key, string field)
        {
            try
            {
                var db = GetDatabase();
                var value = db.HashGet(key, field);
                
                if (value.HasValue)
                {
                    return new APIResult(new
                    {
                        Key = key,
                        Field = field,
                        Value = value.ToString()
                    });
                }
                
                return new APIResult(true, ResultMessage.ErrorTypes.GetData, "Field not found", $"Hash field '{key}.{field}' does not exist");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to get hash field: {Key}.{Field}", key, field);
                return new APIResult(true, ResultMessage.ErrorTypes.GetData, "Error getting hash field", ex.Message);
            }
        }

        public async Task<APIResult> HashGetAsync(string key, string field)
        {
            try
            {
                var db = GetDatabase();
                var value = await db.HashGetAsync(key, field);
                
                if (value.HasValue)
                {
                    return new APIResult(new
                    {
                        Key = key,
                        Field = field,
                        Value = value.ToString()
                    });
                }
                
                return new APIResult(true, ResultMessage.ErrorTypes.GetData, "Field not found", $"Hash field '{key}.{field}' does not exist");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to get hash field (async): {Key}.{Field}", key, field);
                return new APIResult(true, ResultMessage.ErrorTypes.GetData, "Error getting hash field", ex.Message);
            }
        }

        public APIResult HashGetAll(string key)
        {
            try
            {
                var db = GetDatabase();
                var entries = db.HashGetAll(key);
                var dict = entries.ToDictionary(
                    e => e.Name.ToString(),
                    e => e.Value.ToString()
                );
                
                return new APIResult(new
                {
                    Key = key,
                    Count = dict.Count,
                    Fields = dict
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to get all hash fields: {Key}", key);
                return new APIResult(true, ResultMessage.ErrorTypes.GetData, "Error getting hash fields", ex.Message);
            }
        }

        public async Task<APIResult> HashGetAllAsync(string key)
        {
            try
            {
                var db = GetDatabase();
                var entries = await db.HashGetAllAsync(key);
                var dict = entries.ToDictionary(
                    e => e.Name.ToString(),
                    e => e.Value.ToString()
                );
                
                return new APIResult(new
                {
                    Key = key,
                    Count = dict.Count,
                    Fields = dict
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to get all hash fields (async): {Key}", key);
                return new APIResult(true, ResultMessage.ErrorTypes.GetData, "Error getting hash fields", ex.Message);
            }
        }

        public ResultMessage HashDelete(string key, string field)
        {
            try
            {
                var db = GetDatabase();
                bool deleted = db.HashDelete(key, field);
                
                if (deleted)
                {
                    _logger.LogDebug("✅ Deleted hash field: {Key}.{Field}", key, field);
                    return new ResultMessage(false, ResultMessage.ErrorTypes.No_Error, "Hash field deleted successfully");
                }
                
                return new ResultMessage(true, ResultMessage.ErrorTypes.Delete, "Field not found", $"Hash field '{key}.{field}' does not exist");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to delete hash field: {Key}.{Field}", key, field);
                return new ResultMessage(true, ResultMessage.ErrorTypes.Delete, "Error deleting hash field", ex.Message);
            }
        }

        public async Task<ResultMessage> HashDeleteAsync(string key, string field)
        {
            try
            {
                var db = GetDatabase();
                bool deleted = await db.HashDeleteAsync(key, field);
                
                if (deleted)
                {
                    _logger.LogDebug("✅ Deleted hash field (async): {Key}.{Field}", key, field);
                    return new ResultMessage(false, ResultMessage.ErrorTypes.No_Error, "Hash field deleted successfully");
                }
                
                return new ResultMessage(true, ResultMessage.ErrorTypes.Delete, "Field not found", $"Hash field '{key}.{field}' does not exist");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to delete hash field (async): {Key}.{Field}", key, field);
                return new ResultMessage(true, ResultMessage.ErrorTypes.Delete, "Error deleting hash field", ex.Message);
            }
        }
        #endregion

        #region Event Handlers
        
        /// <summary>
        /// Event handler khi Redis connection failed
        /// </summary>
        private void OnConnectionFailed(object? sender, ConnectionFailedEventArgs e)
        {
            var message = $"Redis connection failed: {e.EndPoint} - {e.FailureType}";
            
            // ✅ DUAL LOGGING
            _logger.LogError(message);
            _mongoLogger?.LogError(LOG_CATEGORY, message, null, 
                new { 
                    EndPoint = e.EndPoint.ToString(), 
                    FailureType = e.FailureType.ToString() 
                });
        }

        /// <summary>
        /// Event handler khi Redis connection restored
        /// </summary>
        private void OnConnectionRestored(object? sender, ConnectionFailedEventArgs e)
        {
            var message = $"Redis connection restored: {e.EndPoint}";
            
            // ✅ DUAL LOGGING
            _logger.LogInformation(message);
            _mongoLogger?.LogInformation(LOG_CATEGORY, message, 
                new { EndPoint = e.EndPoint.ToString() });
        }

        /// <summary>
        /// Event handler cho Redis error messages
        /// </summary>
        private void OnErrorMessage(object? sender, RedisErrorEventArgs e)
        {
            // ✅ DUAL LOGGING
            _logger.LogError("Redis error: {Message}", e.Message);
            _mongoLogger?.LogError(LOG_CATEGORY, $"Redis error: {e.Message}");
        }
        
        #endregion

        public void Dispose()
        {
            if (_lazyConnection.IsValueCreated)
            {
                _lazyConnection.Value.Dispose();
            }
        }
    }
}
