using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;
using Shared.Logging.ApiLog;

namespace Shared.Logging;

/// <summary>Tạo index + TTL (idempotent) cho collection log hệ thống và log thực thi API — gọi 1 lần lúc app start.</summary>
public static class MongoLogIndexInitializer
{
    private const int DefaultRetentionDays = 30;
    // Mongo không kết nối được (VM tắt, chưa nối Tailscale) thì bỏ qua sau 5s thay vì treo 30s mặc định
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(5);

    /// <summary>Đọc ConnectionStrings:MongoDB, Database:MongoDatabase/SystemLogsCollection/MongoCollection/LogRetentionDays.</summary>
    public static async Task EnsureLogIndexesAsync(this WebApplication app)
    {
        IConfiguration configuration = app.Configuration;
        string? strConnectionString = configuration.GetConnectionString("MongoDB");
        string? strDatabase = configuration["Database:MongoDatabase"];
        if (string.IsNullOrEmpty(strConnectionString) || string.IsNullOrEmpty(strDatabase))
        {
            Log.Warning("Chưa cấu hình Mongo — bỏ qua tạo index log");
            return;
        }
        IMongoDatabase objDatabase = new MongoClient(strConnectionString).GetDatabase(strDatabase);
        TimeSpan tspRetention = TimeSpan.FromDays(configuration.GetValue("Database:LogRetentionDays", DefaultRetentionDays));
        IndexKeysDefinitionBuilder<BsonDocument> objKeys = Builders<BsonDocument>.IndexKeys;
        // Field theo document của sink Serilog.Sinks.MongoDB
        await EnsureIndexesAsync(objDatabase, configuration["Database:SystemLogsCollection"] ?? LoggingStartupConfig.DefaultSystemLogsCollection,
        [
            new(objKeys.Descending("Timestamp"), new CreateIndexOptions { Name = "idx_timestamp_desc" }),
            new(objKeys.Ascending("Level").Descending("Timestamp"), new CreateIndexOptions { Name = "idx_level_timestamp" }),
            new(objKeys.Ascending("Properties.SourceContext"), new CreateIndexOptions { Name = "idx_properties_sourcecontext" }),
            new(objKeys.Ascending("Properties.Application"), new CreateIndexOptions { Name = "idx_properties_application" }),
            new(objKeys.Ascending("Timestamp"), new CreateIndexOptions { Name = "idx_timestamp_ttl", ExpireAfter = tspRetention })
        ]);
        await EnsureIndexesAsync(objDatabase, configuration["Database:MongoCollection"] ?? ApiLogStartupConfig.DefaultCollection,
        [
            new(objKeys.Descending(nameof(ApiExecutionLog.StartTime)), new CreateIndexOptions { Name = "idx_starttime_desc" }),
            new(objKeys.Descending(nameof(ApiExecutionLog.ExecutionMs)), new CreateIndexOptions { Name = "idx_executionms_desc" }),
            new(objKeys.Ascending(nameof(ApiExecutionLog.CreatedAt)), new CreateIndexOptions { Name = "idx_createdat_ttl", ExpireAfter = tspRetention })
        ]);
    }

    private static async Task EnsureIndexesAsync(IMongoDatabase objDatabase, string strCollection, List<CreateIndexModel<BsonDocument>> lstIndex)
    {
        using CancellationTokenSource objTimeout = new(ConnectTimeout);
        try
        {
            IMongoCollection<BsonDocument> objCollection = objDatabase.GetCollection<BsonDocument>(strCollection);
            List<BsonDocument> lstExisting = await (await objCollection.Indexes.ListAsync(objTimeout.Token)).ToListAsync(objTimeout.Token);
            HashSet<string> hstExistingName = lstExisting.Where(x => x.Contains("name")).Select(x => x["name"].AsString).ToHashSet(StringComparer.OrdinalIgnoreCase);
            List<CreateIndexModel<BsonDocument>> lstCreate = lstIndex.Where(x => !hstExistingName.Contains(x.Options.Name)).ToList();
            if (lstCreate.Count == 0) return;
            await objCollection.Indexes.CreateManyAsync(lstCreate, objTimeout.Token);
            Log.Information("Đã tạo {Count} index cho {Collection}", lstCreate.Count, strCollection);
        }
        catch (OperationCanceledException)
        {
            Log.Warning("Quá {Timeout}s không kết nối được Mongo — bỏ qua tạo index {Collection}", ConnectTimeout.TotalSeconds, strCollection);
        }
        catch (Exception exception)
        {
            // Không chặn app khởi động — index tạo lại được ở lần start sau
            Log.Warning(exception, "Không tạo được index cho {Collection}", strCollection);
        }
    }
}
