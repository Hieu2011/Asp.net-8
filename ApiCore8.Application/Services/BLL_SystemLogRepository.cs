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
    /// Business Logic Layer cho SystemLog
    /// Sử dụng MongoData library + Dual logging
    /// </summary>
    public class BLL_SystemLogRepository : IBLL_SystemLogRepository
    {
        private readonly IMongoDataFactory _mongoFactory;
        private readonly IMongoLoggerService? _mongoLogger; // ✅ Add MongoDB logger
        private readonly string _collectionName;
        private const string LOG_CATEGORY = "BLL_SystemLogRepository"; // ✅ Category

        /// <summary>
        /// Constructor - Inject MongoDataFactory và IMongoLoggerService
        /// </summary>
        public BLL_SystemLogRepository(
            IMongoDataFactory mongoFactory,
            IConfiguration configuration,
            IMongoLoggerService? mongoLogger = null) // ✅ Optional injection
        {
            _mongoFactory = mongoFactory;
            _mongoLogger = mongoLogger; // ✅ Save reference
            _collectionName = configuration["Database:SystemLogsCollection"] ?? "SystemLogs";
        }

        /// <summary>
        /// Search SystemLogs với filter + pagination
        /// </summary>
        public async Task<PagedResult<SystemLog>> SearchAsync(SystemLogFilterRequest request)
        {
            try
            {
                var mongo = _mongoFactory.Create(_collectionName);

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
                    filters.Add(filterBuilder.Regex(x => x.Category, 
                        new BsonRegularExpression(request.Category, "i")));
                }

                if (!string.IsNullOrEmpty(request.Application))
                {
                    filters.Add(filterBuilder.Eq(x => x.Application, request.Application));
                }

                if (!string.IsNullOrEmpty(request.Message))
                {
                    filters.Add(filterBuilder.Text(request.Message));
                }

                if (request.StartDate.HasValue)
                {
                    filters.Add(filterBuilder.Gte(x => x.Timestamp, request.StartDate.Value));
                }

                if (request.EndDate.HasValue)
                {
                    filters.Add(filterBuilder.Lte(x => x.Timestamp, request.EndDate.Value));
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
                            ? sortBuilder.Ascending(x => x.Category)
                            : sortBuilder.Descending(x => x.Category);
                        break;
                    default:
                        sort = request.SortOrder.ToLower() == "asc"
                            ? sortBuilder.Ascending(x => x.Timestamp)
                            : sortBuilder.Descending(x => x.Timestamp);
                        break;
                }

                var pagedResult = await mongo.GetPaged<SystemLog>(
                    filter: finalFilter,
                    page: request.PageIndex,
                    pageSize: request.PageSize,
                    sort: sort
                );

                return pagedResult;
            }
            catch (Exception ex)
            {
                // ✅ DUAL LOGGING
                Log.Error(ex, "Error searching SystemLog");
                _mongoLogger?.LogError(LOG_CATEGORY, "Error searching SystemLog", ex,
                    new { 
                        request.PageIndex, 
                        request.PageSize, 
                        request.Level,
                        request.Category 
                    });

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
        public async Task<(SystemLog?, ResultMessage)> GetLogByIDAsync(string id)
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

                var mongo = _mongoFactory.Create(_collectionName);
                var filter = Builders<SystemLog>.Filter.Eq(x => x.Id, id);
                var log = await mongo.GetOne<SystemLog>(filter);

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
                // ✅ DUAL LOGGING
                Log.Error(ex, "Error getting SystemLog by ID: {Id}", id);
                _mongoLogger?.LogError(LOG_CATEGORY, $"Error getting SystemLog by ID: {id}", ex);

                resultMessage = new ResultMessage(true, ResultMessage.ErrorTypes.GetData,
                    "Error getting log", ex.Message);
                return (null, resultMessage);
            }
        }

        /// <summary>
        /// Xóa logs cũ
        /// </summary>
        public async Task<ResultMessage> DeleteOldLogsAsync(int daysOld)
        {
            try
            {
                var mongo = _mongoFactory.Create(_collectionName);
                var cutoffDate = DateTime.Now.AddDays(-daysOld);
                var filter = Builders<SystemLog>.Filter.Lt(x => x.Timestamp, cutoffDate);
                var deletedCount = await mongo.DeleteManyAsync<SystemLog>(filter);

                // ✅ DUAL LOGGING
                Log.Information("Deleted {Count} old system logs (older than {Days} days)", deletedCount, daysOld);
                _mongoLogger?.LogInformation(LOG_CATEGORY,
                    $"Deleted {deletedCount} old system logs (older than {daysOld} days)",
                    new { deletedCount, daysOld, cutoffDate });

                return new ResultMessage(false, ResultMessage.ErrorTypes.No_Error,
                    "Deleted successfully",
                    $"Deleted {deletedCount} old logs");
            }
            catch (Exception ex)
            {
                // ✅ DUAL LOGGING
                Log.Error(ex, "Error deleting old system logs");
                _mongoLogger?.LogError(LOG_CATEGORY, "Error deleting old system logs", ex,
                    new { daysOld });

                return new ResultMessage(true, ResultMessage.ErrorTypes.Delete,
                    "Error deleting old logs", ex.Message);
            }
        }
    }
}