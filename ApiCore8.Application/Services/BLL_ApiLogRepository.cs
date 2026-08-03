using ApiCore8.Application.Abstractions;
using ApiCore8.Application.Contracts;
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
    /// Sử dụng MongoData library + Dual logging (Serilog + MongoDB)
    /// </summary>
    public class BLL_ApiLogRepository : IBLL_ApiLogRepository
    {
        private readonly IMongoLoggerService? _mongoLogger; // ✅ Add MongoDB logger
        private readonly string _collectionName;
        private const string LOG_CATEGORY = "BLL_ApiLogRepository"; // ✅ Category
        private readonly IMongoData _mongo;
        /// <summary>
        /// Constructor - Inject MongoDataFactory và IMongoLoggerService
        /// </summary>
        public BLL_ApiLogRepository(
            IConfiguration configuration,
            IMongoDataFactory mongoFactory,
            IMongoLoggerService? mongoLogger = null) // ✅ Optional injection
        {
            _mongoLogger = mongoLogger; // ✅ Save reference
            _collectionName = configuration["Database:MongoCollection"] ?? "APILogs";
            _mongo = mongoFactory.Create(_collectionName);
        }

        /// <summary>
        /// Insert ApiExecutionLog vào MongoDB
        /// </summary>
        public async Task<bool> InsertLog(ApiExecutionLog log)
        {
            try
            {
                return await _mongo.Insert(log);
            }
            catch (Exception ex)
            {
                // ✅ DUAL LOGGING: Serilog + MongoDB
                Log.Error(ex, "Error inserting ApiExecutionLog");
                _mongoLogger?.LogError(LOG_CATEGORY, "Error inserting ApiExecutionLog", ex);
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
                var pagedResult = await _mongo.GetPaged<ApiExecutionLog>(
                    filter: finalFilter,
                    page: request.Page,
                    pageSize: request.PageSize,
                    sort: sort
                );

                return pagedResult;
            }
            catch (Exception ex)
            {
                // ✅ DUAL LOGGING với metadata
                Log.Error(ex, "Error searching ApiExecutionLog");
                _mongoLogger?.LogError(LOG_CATEGORY, "Error searching ApiExecutionLog", ex, 
                    new { 
                        request.Page, 
                        request.PageSize, 
                        request.ApiName,
                        request.Method 
                    });
                
                return new PagedResult<ApiExecutionLog>
                {
                    Items = new List<ApiExecutionLog>(),
                    Total = 0,
                    Page = request.Page,
                    PageSize = request.PageSize
                };
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
                var log = await _mongo.GetOne<ApiExecutionLog>(filter);

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
                // ✅ DUAL LOGGING
                Log.Error(ex, "Error getting ApiExecutionLog by ID: {Id}", id);
                _mongoLogger?.LogError(LOG_CATEGORY, $"Error getting ApiExecutionLog by ID: {id}", ex);
                
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

                return await _mongo.GetPaged<ApiExecutionLog>(
                    filter: filter,
                    page: pageIndex,
                    pageSize: pageSize,
                    sort: sort
                );
            }
            catch (Exception ex)
            {
                // ✅ DUAL LOGGING
                Log.Error(ex, "Error getting slow logs");
                _mongoLogger?.LogError(LOG_CATEGORY, "Error getting slow logs", ex, 
                    new { thresholdMs, pageIndex, pageSize });
                
                return new PagedResult<ApiExecutionLog>
                {
                    Items = new List<ApiExecutionLog>(),
                    Total = 0,
                    Page = pageIndex,
                    PageSize = pageSize
                };
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
                var deletedCount = await _mongo.DeleteManyAsync<ApiExecutionLog>(filter);

                // ✅ DUAL LOGGING cho success
                Log.Information("Deleted {Count} old logs (older than {Days} days)", deletedCount, daysOld);
                _mongoLogger?.LogInformation(LOG_CATEGORY, 
                    $"Deleted {deletedCount} old logs (older than {daysOld} days)",
                    new { deletedCount, daysOld, cutoffDate });

                return new ResultMessage(false, ResultMessage.ErrorTypes.No_Error,
                    "Deleted successfully",
                    $"Deleted {deletedCount} old logs");
            }
            catch (Exception ex)
            {
                // ✅ DUAL LOGGING for errors
                Log.Error(ex, "Error deleting old logs");
                _mongoLogger?.LogError(LOG_CATEGORY, "Error deleting old logs", ex, 
                    new { daysOld });
                
                return new ResultMessage(true, ResultMessage.ErrorTypes.Delete,
                    "Error deleting old logs", ex.Message);
            }
        }
    }
}
