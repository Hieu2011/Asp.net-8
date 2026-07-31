using Core.Database;
using Microsoft.Extensions.Configuration;
using ML;
using MongoDB.Driver;
using System.Collections.Concurrent;

namespace Core.Logging
{
    /// <summary>
    /// MongoDB Logger Service - Ghi system logs vào MongoDB
    /// Sử dụng MongoData library + batch insert optimization
    /// </summary>
    public class MongoLoggerService : IMongoLoggerService, IDisposable
    {
        private readonly MongoClient _mongoClient;
        private readonly IConfiguration _configuration; // Lưu lại config
        private readonly string _collectionName;
        private static readonly ConcurrentQueue<SystemLog> _logQueue = new();
        private readonly Timer _flushTimer;
        private readonly bool _enabled;

        /// <summary>
        /// Constructor
        /// </summary>
        public MongoLoggerService(
            MongoClient mongoClient,
            IConfiguration configuration)
        {
            _mongoClient = mongoClient;
            _configuration = configuration; // ✅ Lưu config để lấy thông tin DB
            _collectionName = configuration["Database:SystemLogsCollection"] ?? "SystemLogs";
            _enabled = configuration.GetValue<bool>("Logging:EnableMongoLogging", true);

            // Auto-flush mỗi 2 giây
            _flushTimer = new Timer(async _ => await FlushAsync(), null, 
                TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));

            // Tạo indexes khi khởi tạo
            Task.Run(async () => await EnsureIndexesAsync());
        }

        // ✅ Hàm helper tự tạo MongoData mà không cần Factory
        private IMongoData CreateMongoConnection()
        {
            string connectionString = _configuration.GetConnectionString("MongoDB") ?? "";
            string databaseName = _configuration["Database:MongoDatabase"] ?? "DefaultDB";
            
            // Tạo trực tiếp instance, truyền logger là null để tránh bị đệ quy
            var mongo = new MongoData(_mongoClient, connectionString, databaseName, null);
            mongo.AddCollection(_collectionName);
            mongo.Connect();
            return mongo;
        }

        /// <summary>
        /// Tạo indexes cho SystemLogs collection
        /// </summary>
        public async Task EnsureIndexesAsync()
        {
            try
            {
                // ✅ CẢI TIẾN: 1 lệnh duy nhất
                var mongo = CreateMongoConnection(); // ✅ Dùng hàm helper

                // Định nghĩa indexes
                var indexModels = new List<CreateIndexModel<SystemLog>>
                {
                    // Index cho sorting by timestamp
                    new CreateIndexModel<SystemLog>(
                        Builders<SystemLog>.IndexKeys.Descending(x => x.Timestamp),
                        new CreateIndexOptions { Name = "idx_timestamp_desc", Background = true }
                    ),

                    // Compound index: level + timestamp
                    new CreateIndexModel<SystemLog>(
                        Builders<SystemLog>.IndexKeys
                            .Ascending(x => x.Level)
                            .Descending(x => x.Timestamp),
                        new CreateIndexOptions { Name = "idx_level_timestamp", Background = true }
                    ),

                    // Index cho category
                    new CreateIndexModel<SystemLog>(
                        Builders<SystemLog>.IndexKeys.Ascending(x => x.Category),
                        new CreateIndexOptions { Name = "idx_category", Background = true }
                    ),

                    // Compound index: category + timestamp
                    new CreateIndexModel<SystemLog>(
                        Builders<SystemLog>.IndexKeys
                            .Ascending(x => x.Category)
                            .Descending(x => x.Timestamp),
                        new CreateIndexOptions { Name = "idx_category_timestamp", Background = true }
                    ),

                    // Text index cho message search
                    new CreateIndexModel<SystemLog>(
                        Builders<SystemLog>.IndexKeys.Text(x => x.Message),
                        new CreateIndexOptions { Name = "idx_message_text", Background = true }
                    ),

                    // Compound text index: message + exception
                    new CreateIndexModel<SystemLog>(
                        Builders<SystemLog>.IndexKeys.Combine(
                            Builders<SystemLog>.IndexKeys.Text(x => x.Message),
                            Builders<SystemLog>.IndexKeys.Text(x => x.Exception)
                        ),
                        new CreateIndexOptions { Name = "idx_message_exception_text", Background = true }
                    ),

                    // Index cho application
                    new CreateIndexModel<SystemLog>(
                        Builders<SystemLog>.IndexKeys.Ascending(x => x.Application),
                        new CreateIndexOptions { Name = "idx_application", Background = true }
                    ),

                    // TTL Index - Auto delete logs sau 30 ngày
                    new CreateIndexModel<SystemLog>(
                        Builders<SystemLog>.IndexKeys.Ascending(x => x.Timestamp),
                        new CreateIndexOptions 
                        { 
                            Name = "idx_timestamp_ttl",
                            ExpireAfter = TimeSpan.FromDays(30),
                            Background = true
                        }
                    )
                };

                // ✅ SỬ DỤNG MongoData.CreateIndex()
                await mongo.CreateIndex(indexModels);
                
                Console.WriteLine("✅ SystemLogs indexes created successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Warning: Failed to create SystemLogs indexes: {ex.Message}");
            }
        }

        #region Log Methods

        public void LogInformation(string category, string message, object? data = null)
        {
            Log("Information", category, message, null, data);
        }

        public void LogWarning(string category, string message, object? data = null)
        {
            Log("Warning", category, message, null, data);
        }

        public void LogError(string category, string message, Exception? exception = null, object? data = null)
        {
            Log("Error", category, message, exception, data);
        }

        public void LogDebug(string category, string message, object? data = null)
        {
            Log("Debug", category, message, null, data);
        }

        public void LogCritical(string category, string message, Exception? exception = null, object? data = null)
        {
            Log("Critical", category, message, exception, data);
        }

        private void Log(string level, string category, string message, Exception? exception, object? data)
        {
            if (!_enabled)
                return;

            var log = new SystemLog
            {
                Timestamp = DateTime.Now,
                Level = level,
                Category = category,
                Message = message,
                Exception = exception?.Message,
                StackTrace = exception?.StackTrace,
                ScopeData = data != null ? new Dictionary<string, object> { ["data"] = data } : null
            };

            _logQueue.Enqueue(log);
        }

        #endregion

        /// <summary>
        /// Flush logs từ queue vào MongoDB (batch insert)
        /// </summary>
        public async Task FlushAsync()
        {
            if (_logQueue.IsEmpty)
                return;

            var logsToInsert = new List<SystemLog>();

            // Dequeue tối đa 100 logs mỗi lần
            while (_logQueue.TryDequeue(out var log) && logsToInsert.Count < 100)
            {
                logsToInsert.Add(log);
            }

            if (logsToInsert.Count > 0)
            {
                try
                {
                    // ✅ CẢI TIẾN: 1 lệnh duy nhất
                    var mongo = CreateMongoConnection(); // ✅ Dùng hàm helper
                    
                    // ✅ SỬ DỤNG MongoData.InsertMany() - BATCH INSERT
                    await mongo.InsertMany(logsToInsert);
                }
                catch
                {
                    // Nếu insert fail, re-enqueue để retry
                    foreach (var log in logsToInsert)
                    {
                        _logQueue.Enqueue(log);
                    }
                }
            }
        }

        public void Dispose()
        {
            _flushTimer?.Dispose();
            FlushAsync().GetAwaiter().GetResult();
        }
    }
}