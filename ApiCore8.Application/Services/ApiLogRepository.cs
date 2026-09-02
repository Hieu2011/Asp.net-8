using System.Text.RegularExpressions;
using ApiCore8.Application.Contracts;
using ApiCore8.Application.Extensions;
using ApiCore8.Application.Interfaces;
using ApiCore8.Domain.Entities;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;

namespace ApiCore8.Application.Services
{
    /// <summary>
    /// Business Logic Layer cho ApiExecutionLog
    /// </summary>
    public class ApiLogRepository : IApiLogRepository
    {
        private static readonly ILogger _log = Log.ForContext<ApiLogRepository>();

        private readonly IMongoCollection<ApiExecutionLog> _collection;

        public ApiLogRepository(
            IConfiguration configuration,
            IMongoDatabase database)
        {
            var collectionName = configuration["Database:MongoCollection"] ?? "APILogs";
            _collection = database.GetCollection<ApiExecutionLog>(collectionName);
        }

        /// <summary>
        /// Insert ApiExecutionLog vào MongoDB
        /// </summary>
        public async Task<bool> InsertLog(ApiExecutionLog log)
        {
            try
            {
                await _collection.InsertOneAsync(log);
                return true;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error inserting ApiExecutionLog");
                return false;
            }
        }

        /// <summary>
        /// Search ApiExecutionLogs với filter + pagination
        /// </summary>
        public async Task<PagedResult<ApiExecutionLog>> Search(LogFilterRequest request)
        {
            try
            {
                // Build filter từ request
                var filterBuilder = Builders<ApiExecutionLog>.Filter;
                var filters = new List<FilterDefinition<ApiExecutionLog>>();

                // Filter by ID
                if (!string.IsNullOrEmpty(request.Id) && ObjectId.TryParse(request.Id, out var objectId))
                {
                    filters.Add(filterBuilder.Eq(x => x.Id, request.Id));
                }

                // Filter by ApiName (regex cho partial match)
                if (!string.IsNullOrEmpty(request.ApiName))
                {
                    filters.Add(filterBuilder.Regex(x => x.ApiName,
                        new BsonRegularExpression(request.ApiName, "i")));
                }

                // Filter by Method
                if (!string.IsNullOrEmpty(request.Method))
                {
                    filters.Add(filterBuilder.Eq(x => x.Method, request.Method.ToUpper()));
                }

                // Filter by Date Range
                if (request.From.HasValue)
                {
                    filters.Add(filterBuilder.Gte(x => x.CreatedAt, request.From.Value));
                }

                if (request.To.HasValue)
                {
                    filters.Add(filterBuilder.Lte(x => x.CreatedAt, request.To.Value));
                }

                // Text search
                if (!string.IsNullOrEmpty(request.Keyword))
                {
                    filters.Add(filterBuilder.Text(request.Keyword));
                }

                // Combine filters
                var finalFilter = filters.Count > 0
                    ? filterBuilder.And(filters)
                    : filterBuilder.Empty;

                // Build sort
                var sort = Builders<ApiExecutionLog>.Sort.Descending(x => x.CreatedAt);

                // Execute query
                var pagedResult = await _collection.GetPagedAsync(
                    filter: finalFilter,
                    page: request.Page,
                    pageSize: request.PageSize,
                    sort: sort
                );

                return pagedResult;
            }
            catch (Exception ex)
            {
                // Rethrow (không trả PagedResult rỗng) — PagedResult không có field chứa lỗi, nuốt ở
                // đây sẽ khiến controller không bao giờ thấy exception để đưa ex.Message vào MessageDetail.
                _log.Error(ex, "Error searching ApiExecutionLog");
                throw;
            }
        }

        /// <summary>
        /// Search 1 từ khóa duy nhất — khớp LIKE (regex, không phân biệt hoa thường) trên bất kỳ
        /// field nào trong ApiName/RequestBody/ResponseBody (OR giữa 3 field), kết hợp AND với
        /// FromDate/ToDate nếu có truyền. Regex.Escape trước để tìm theo từ khóa, không phải regex
        /// tùy ý (tránh input người dùng chứa ký tự regex đặc biệt làm Mongo từ chối pattern).
        /// FromDate lọc theo StartTime, ToDate lọc theo EndTime — đúng khoảng thời gian request đó
        /// THỰC SỰ chạy, không dùng CreatedAt (chỉ là thời điểm ghi log, luôn xấp xỉ EndTime).
        /// </summary>
        public async Task<PagedResult<ApiExecutionLog>> SearchByKeywordAsync(ApiLogKeywordSearchRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var filterBuilder = Builders<ApiExecutionLog>.Filter;
                var filters = new List<FilterDefinition<ApiExecutionLog>>();

                if (!string.IsNullOrEmpty(request.Keyword))
                {
                    var pattern = new BsonRegularExpression(Regex.Escape(request.Keyword), "i");
                    filters.Add(filterBuilder.Or(
                        filterBuilder.Regex(x => x.ApiName, pattern),
                        filterBuilder.Regex(x => x.RequestBody, pattern),
                        filterBuilder.Regex(x => x.ResponseBody, pattern)
                    ));
                }

                if (request.FromDate.HasValue)
                {
                    filters.Add(filterBuilder.Gte(x => x.StartTime, request.FromDate.Value));
                }

                if (request.ToDate.HasValue)
                {
                    filters.Add(filterBuilder.Lte(x => x.EndTime, request.ToDate.Value));
                }

                var finalFilter = filters.Count > 0
                    ? filterBuilder.And(filters)
                    : filterBuilder.Empty;

                var sort = Builders<ApiExecutionLog>.Sort.Descending(x => x.CreatedAt);

                return await _collection.GetPagedAsync(
                    filter: finalFilter,
                    page: request.Page,
                    pageSize: request.PageSize,
                    sort: sort,
                    cancellationToken: cancellationToken
                );
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error searching ApiExecutionLog by keyword");
                throw;
            }
        }

        /// <summary>
        /// Lấy ApiExecutionLog theo ID
        /// </summary>
        public async Task<(ApiExecutionLog?, ResultMessage)> GetLogByID(string id)
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

                var filter = Builders<ApiExecutionLog>.Filter.Eq(x => x.Id, id);
                var log = await _collection.Find(filter).FirstOrDefaultAsync();

                if (log == null)
                {
                    resultMessage = new ResultMessage(true, ResultMessage.ErrorTypes.GetData,
                        "Not found", $"ApiExecutionLog with ID '{id}' not found");
                    return (null, resultMessage);
                }

                return (log, resultMessage);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error getting ApiExecutionLog by ID: {Id}", id);

                resultMessage = new ResultMessage(true, ResultMessage.ErrorTypes.GetData,
                    "Error getting log", ex.Message);
                return (null, resultMessage);
            }
        }

        /// <summary>
        /// Lấy slow logs (ExecutionMs > threshold)
        /// </summary>
        public async Task<PagedResult<ApiExecutionLog>> GetSlowLogs(long thresholdMs = 1000, int pageIndex = 1, int pageSize = 20)
        {
            try
            {
                var filter = Builders<ApiExecutionLog>.Filter.Gte(x => x.ExecutionMs, thresholdMs);
                var sort = Builders<ApiExecutionLog>.Sort.Descending(x => x.ExecutionMs);

                return await _collection.GetPagedAsync(
                    filter: filter,
                    page: pageIndex,
                    pageSize: pageSize,
                    sort: sort
                );
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error getting slow logs");
                throw;
            }
        }

        /// <summary>
        /// Xóa logs cũ hơn số ngày chỉ định
        /// </summary>
        public async Task<ResultMessage> DeleteOldLogs(int daysOld)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-daysOld);
                var filter = Builders<ApiExecutionLog>.Filter.Lt(x => x.CreatedAt, cutoffDate);
                var deletedCount = (await _collection.DeleteManyAsync(filter)).DeletedCount;

                _log.Information("Deleted {Count} old logs (older than {Days} days)", deletedCount, daysOld);

                return new ResultMessage(false, ResultMessage.ErrorTypes.No_Error,
                    "Deleted successfully",
                    $"Deleted {deletedCount} old logs");
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error deleting old logs");

                return new ResultMessage(true, ResultMessage.ErrorTypes.Delete,
                    "Error deleting old logs", ex.Message);
            }
        }
    }
}
