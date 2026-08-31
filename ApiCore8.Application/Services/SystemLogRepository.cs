using ApiCore8.Application.Contracts;
using ApiCore8.Application.Extensions;
using ApiCore8.Application.Interfaces;
using ApiCore8.Domain.Entities;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;
using System.Text.RegularExpressions;

namespace ApiCore8.Application.Services
{
    /// <summary>
    /// Business Logic Layer cho SystemLog
    /// </summary>
    public class SystemLogRepository : ISystemLogRepository
    {
        /// <summary>
        /// Tên collection mặc định nếu "Database:SystemLogsCollection" không được cấu hình —
        /// dùng chung hằng số này ở mọi nơi cần biết tên collection (vd: sink Mongo của Serilog
        /// trong LoggingStartupConfig) để tránh 2 nơi hardcode lệch nhau.
        /// </summary>
        public const string DefaultCollectionName = "SystemLogs";

        // Gắn SourceContext = tên class này, để lọc/sort SystemLogs theo Category (đọc từ
        // Properties.SourceContext) không bỏ sót log ghi từ chính repository này.
        private static readonly ILogger _log = Log.ForContext<SystemLogRepository>();

        private readonly IMongoCollection<SystemLog> _collection;

        public SystemLogRepository(
            IMongoDatabase database,
            IConfiguration configuration)
        {
            var collectionName = configuration["Database:SystemLogsCollection"] ?? DefaultCollectionName;
            _collection = database.GetCollection<SystemLog>(collectionName);
        }

        /// <summary>
        /// Search SystemLogs với filter + pagination
        /// </summary>
        public async Task<PagedResult<SystemLog>> SearchAsync(SystemLogFilterRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                // Build filter
                var filterBuilder = Builders<SystemLog>.Filter;
                var filters = new List<FilterDefinition<SystemLog>>();

                if (!string.IsNullOrEmpty(request.Id) && ObjectId.TryParse(request.Id, out var objectId))
                {
                    filters.Add(filterBuilder.Eq(x => x.Id, request.Id));
                }

                if (!string.IsNullOrEmpty(request.Level))
                {
                    filters.Add(filterBuilder.Eq(x => x.Level, request.Level));
                }

                if (!string.IsNullOrEmpty(request.Category))
                {
                    // "Category" maps to Serilog's SourceContext property (the emitting class name),
                    // which the sink stores nested under Properties rather than as a top-level field.
                    filters.Add(filterBuilder.Regex("Properties.SourceContext",
                        new BsonRegularExpression(request.Category, "i")));
                }

                if (!string.IsNullOrEmpty(request.Application))
                {
                    filters.Add(filterBuilder.Eq("Properties.Application", request.Application));
                }

                if (!string.IsNullOrEmpty(request.Message))
                {
                    // Regex.Escape trước khi build pattern — request.Message là input người dùng,
                    // nếu để nguyên thì 1 ký tự regex đặc biệt không hợp lệ (vd dấu "(" lẻ) sẽ khiến
                    // Mongo từ chối pattern, exception bị catch nuốt mất, trả về rỗng thay vì báo lỗi.
                    // Escape cũng biến tìm kiếm này thành đúng nghĩa "tìm theo từ khóa", không phải
                    // "tìm theo regex" — đúng ý định của field Message hơn.
                    filters.Add(filterBuilder.Regex(x => x.Message,
                        new BsonRegularExpression(Regex.Escape(request.Message), "i")));
                }

                // Timestamp lưu theo UTC (sink Serilog ghi UtcTimeStamp) — quy đổi StartDate/EndDate
                // về UTC trước khi so sánh, tránh lệch theo múi giờ server nếu caller gửi giờ local.
                if (request.StartDate.HasValue)
                {
                    filters.Add(filterBuilder.Gte(x => x.Timestamp, ToUtc(request.StartDate.Value)));
                }

                if (request.EndDate.HasValue)
                {
                    filters.Add(filterBuilder.Lte(x => x.Timestamp, ToUtc(request.EndDate.Value)));
                }

                var finalFilter = filters.Count > 0
                    ? filterBuilder.And(filters)
                    : filterBuilder.Empty;

                // Build sort
                var sortBuilder = Builders<SystemLog>.Sort;
                SortDefinition<SystemLog> sort;

                switch (request.SortBy.ToLower())
                {
                    case "level":
                        sort = request.SortOrder.ToLower() == "asc"
                            ? sortBuilder.Ascending(x => x.Level)
                            : sortBuilder.Descending(x => x.Level);
                        break;
                    case "category":
                        sort = request.SortOrder.ToLower() == "asc"
                            ? sortBuilder.Ascending("Properties.SourceContext")
                            : sortBuilder.Descending("Properties.SourceContext");
                        break;
                    default:
                        sort = request.SortOrder.ToLower() == "asc"
                            ? sortBuilder.Ascending(x => x.Timestamp)
                            : sortBuilder.Descending(x => x.Timestamp);
                        break;
                }

                var pagedResult = await _collection.GetPagedAsync(
                    filter: finalFilter,
                    page: request.PageIndex,
                    pageSize: request.PageSize,
                    sort: sort,
                    cancellationToken: cancellationToken
                );

                return pagedResult;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error searching SystemLog");

                return new PagedResult<SystemLog>
                {
                    Items = new List<SystemLog>(),
                    Total = 0,
                    Page = request.PageIndex,
                    PageSize = request.PageSize
                };
            }
        }

        /// <summary>
        /// Lấy SystemLog theo ID
        /// </summary>
        public async Task<(SystemLog?, ResultMessage)> GetLogByIDAsync(string id, CancellationToken cancellationToken = default)
        {
            var resultMessage = new ResultMessage();

            try
            {
                if (!ObjectId.TryParse(id, out var objectId))
                {
                    resultMessage = new ResultMessage(true, ResultMessage.ErrorTypes.GetData,
                        "Invalid ID", "ID is not a valid ObjectId");
                    return (null, resultMessage);
                }

                var filter = Builders<SystemLog>.Filter.Eq(x => x.Id, id);
                var log = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

                if (log == null)
                {
                    resultMessage = new ResultMessage(true, ResultMessage.ErrorTypes.GetData,
                        "Not found", $"SystemLog with ID '{id}' not found");
                    return (null, resultMessage);
                }

                return (log, resultMessage);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error getting SystemLog by ID: {Id}", id);

                resultMessage = new ResultMessage(true, ResultMessage.ErrorTypes.GetData,
                    "Error getting log", ex.Message);
                return (null, resultMessage);
            }
        }

        /// <summary>
        /// Xóa logs cũ
        /// </summary>
        public async Task<ResultMessage> DeleteOldLogsAsync(int daysOld, CancellationToken cancellationToken = default)
        {
            try
            {
                // Timestamp lưu UTC — phải trừ ngày trên UtcNow, không phải giờ local của server.
                var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);
                var filter = Builders<SystemLog>.Filter.Lt(x => x.Timestamp, cutoffDate);
                var deletedCount = (await _collection.DeleteManyAsync(filter, cancellationToken)).DeletedCount;

                _log.Information("Deleted {Count} old system logs (older than {Days} days)", deletedCount, daysOld);

                return new ResultMessage(false, ResultMessage.ErrorTypes.No_Error,
                    "Deleted successfully",
                    $"Deleted {deletedCount} old logs");
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error deleting old system logs");

                return new ResultMessage(true, ResultMessage.ErrorTypes.Delete,
                    "Error deleting old logs", ex.Message);
            }
        }

        /// <summary>
        /// Xóa 1 log theo đúng ID (khác DeleteOldLogsAsync — xóa hàng loạt theo số ngày).
        /// </summary>
        public async Task<ResultMessage> DeleteByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ObjectId.TryParse(id, out _))
                {
                    return new ResultMessage(true, ResultMessage.ErrorTypes.Delete,
                        "Invalid ID", "ID is not a valid ObjectId");
                }

                var filter = Builders<SystemLog>.Filter.Eq(x => x.Id, id);
                var deletedCount = (await _collection.DeleteOneAsync(filter, cancellationToken)).DeletedCount;

                if (deletedCount == 0)
                {
                    return new ResultMessage(true, ResultMessage.ErrorTypes.Delete,
                        "Not found", $"SystemLog with ID '{id}' not found");
                }

                _log.Information("Deleted SystemLog {Id}", id);

                return new ResultMessage(false, ResultMessage.ErrorTypes.No_Error,
                    "Deleted successfully", $"Deleted log {id}");
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error deleting SystemLog by ID: {Id}", id);

                return new ResultMessage(true, ResultMessage.ErrorTypes.Delete,
                    "Error deleting log", ex.Message);
            }
        }

        /// <summary>
        /// Thêm 1 SystemLog thủ công — dùng để test Search/GetById/Delete mà không cần đợi
        /// app tự sinh log thật qua Serilog.
        /// </summary>
        public async Task<ResultMessage> InsertAsync(SystemLog log, CancellationToken cancellationToken = default)
        {
            try
            {
                await _collection.InsertOneAsync(log, options: null, cancellationToken);

                _log.Information("Inserted test SystemLog {Id}", log.Id);

                return new ResultMessage(false, ResultMessage.ErrorTypes.No_Error,
                    "Inserted successfully", log.Id);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error inserting SystemLog");

                return new ResultMessage(true, ResultMessage.ErrorTypes.Insert,
                    "Error inserting log", ex.Message);
            }
        }

        /// <summary>
        /// Quy đổi DateTime người dùng truyền vào (thường là giờ local, không rõ Kind) sang UTC
        /// để so sánh đúng với Timestamp lưu trong Mongo (luôn là UTC).
        /// </summary>
        private static DateTime ToUtc(DateTime value) =>
            value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
    }
}
