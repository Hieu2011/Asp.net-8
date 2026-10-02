using ApiCore8.Domain.Entities;
using MongoDB.Driver;
using Serilog;

namespace ApiCore8.Infrastructure.Mongo
{
    /// <summary>
    /// Tạo index (idempotent) cho collection SystemLogs — thay thế phần index/TTL mà
    /// MongoLoggerService (đã xóa) từng tự làm mỗi lần khởi động. Gọi 1 lần lúc app start.
    /// </summary>
    public static class MongoIndexInitializer
    {
        // Timeout ngắn thay cho mặc định 30s của MongoDB driver — nếu Mongo không kết nối được
        // (vd đang dev local, VM chưa bật/chưa nối Tailscale), app không nên treo cả nửa phút
        // ở đúng bước này lúc khởi động, chỉ log cảnh báo rồi chạy tiếp.
        private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(5);

        public static async Task EnsureSystemLogIndexesAsync(IMongoDatabase database, string collectionName)
        {
            using var cts = new CancellationTokenSource(ConnectTimeout);

            try
            {
                var collection = database.GetCollection<SystemLog>(collectionName);
                var keys = Builders<SystemLog>.IndexKeys;

                var models = new List<CreateIndexModel<SystemLog>>
                {
                    new(keys.Descending(x => x.Timestamp),
                        new CreateIndexOptions { Name = "idx_timestamp_desc", Background = true }),

                    new(keys.Ascending(x => x.Level).Descending(x => x.Timestamp),
                        new CreateIndexOptions { Name = "idx_level_timestamp", Background = true }),

                    new(keys.Ascending("Properties.SourceContext"),
                        new CreateIndexOptions { Name = "idx_properties_sourcecontext", Background = true }),

                    new(keys.Ascending("Properties.Application"),
                        new CreateIndexOptions { Name = "idx_properties_application", Background = true }),

                    // TTL — tự xóa log cũ hơn 30 ngày, tránh SystemLogs phình vô hạn.
                    new(keys.Ascending(x => x.Timestamp),
                        new CreateIndexOptions { Name = "idx_timestamp_ttl", ExpireAfter = TimeSpan.FromDays(30), Background = true }),
                };

                var existingNames = (await (await collection.Indexes.ListAsync(cts.Token)).ToListAsync(cts.Token))
                    .Where(d => d.Contains("name"))
                    .Select(d => d["name"].AsString)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var toCreate = models.Where(m => !existingNames.Contains(m.Options.Name)).ToList();
                if (toCreate.Count > 0)
                {
                    await collection.Indexes.CreateManyAsync(toCreate, cancellationToken: cts.Token);
                    Log.Information("Created {Count} indexes for {Collection}", toCreate.Count, collectionName);
                }
            }
            catch (OperationCanceledException)
            {
                Log.Warning("Timed out after {Timeout}s ensuring indexes for {Collection} — MongoDB unreachable, skipping",
                    ConnectTimeout.TotalSeconds, collectionName);
            }
            catch (Exception ex)
            {
                // Không tạo được index (vd chưa có quyền) không nên chặn app khởi động —
                // chỉ log cảnh báo, index có thể tạo lại thủ công hoặc ở lần khởi động sau.
                Log.Warning(ex, "Failed to ensure indexes for {Collection}", collectionName);
            }
        }
    }
}
