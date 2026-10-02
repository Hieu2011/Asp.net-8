using Shared.Common.Contracts;
using ApiCore8.Application.Contracts;
using ApiCore8.Application.Interfaces;
using ApiCore8.Domain.Entities;
using MongoDB.Bson;

namespace ApiCore8.Application.Services
{
    /// <inheritdoc cref="ISystemLogUseCase" />
    public class SystemLogUseCase : ISystemLogUseCase
    {
        private readonly ISystemLogRepository _logRepository;

        public SystemLogUseCase(ISystemLogRepository logRepository)
        {
            _logRepository = logRepository;
        }

        public async Task<APIResult> InsertAsync(InsertSystemLogRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var log = new SystemLog
                {
                    Timestamp = DateTime.UtcNow,
                    Level = request.Level,
                    Message = request.Message,
                    Properties = string.IsNullOrEmpty(request.Category)
                        ? null
                        : new BsonDocument { { "SourceContext", request.Category } }
                };

                var result = await _logRepository.InsertAsync(log, cancellationToken);
                if (result.IsError)
                {
                    return new APIResult(true, result.ErrorType, result.Message, result.MessageDetail);
                }

                return new APIResult(new { Id = log.Id, Message = "Inserted successfully" });
            }
            catch (Exception ex)
            {
                return new APIResult(true, ResultMessage.ErrorTypes.Insert, "Error inserting system log", ex.Message);
            }
        }

        public async Task<APIResult> DeleteByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _logRepository.DeleteByIdAsync(id, cancellationToken);
                if (result.IsError)
                {
                    return new APIResult(true, result.ErrorType, result.Message, result.MessageDetail);
                }

                return new APIResult(new { Message = result.MessageDetail });
            }
            catch (Exception ex)
            {
                return new APIResult(true, ResultMessage.ErrorTypes.Delete, "Error deleting system log", ex.Message);
            }
        }

        public async Task<APIResult> SearchAsync(SystemLogFilterRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (request == null)
                {
                    return new APIResult(true, ResultMessage.ErrorTypes.CheckData, "Invalid request", "Request body cannot be null");
                }

                if (request.PageIndex < 1)
                    request.PageIndex = 1;

                if (request.PageSize < 1 || request.PageSize > 100)
                    request.PageSize = 20;

                var pagedResult = await _logRepository.SearchAsync(request, cancellationToken);
                return new APIResult(pagedResult);
            }
            catch (Exception ex)
            {
                return new APIResult(true, ResultMessage.ErrorTypes.SearchData, "Error searching system logs", ex.Message);
            }
        }

        public async Task<APIResult> GetLogByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return new APIResult(true, ResultMessage.ErrorTypes.GetData, "Invalid request", "Id is required");
                }

                if (!ObjectId.TryParse(id, out _))
                {
                    return new APIResult(true, ResultMessage.ErrorTypes.GetData, "Invalid request", "Id is not a valid ObjectId");
                }

                var (log, resultMessage) = await _logRepository.GetLogByIDAsync(id, cancellationToken);
                if (resultMessage.IsError)
                {
                    return new APIResult(true, resultMessage.ErrorType, resultMessage.Message, resultMessage.MessageDetail);
                }

                return new APIResult(log);
            }
            catch (Exception ex)
            {
                return new APIResult(true, ResultMessage.ErrorTypes.GetData, "Error getting system log", ex.Message);
            }
        }

        public async Task<APIResult> GetRecentAsync(int limit, CancellationToken cancellationToken = default)
        {
            try
            {
                if (limit < 1) limit = 50;
                if (limit > 100) limit = 100;

                var request = new SystemLogFilterRequest
                {
                    PageIndex = 1,
                    PageSize = limit,
                    SortBy = "timestamp",
                    SortOrder = "desc"
                };

                var pagedResult = await _logRepository.SearchAsync(request, cancellationToken);
                return new APIResult(pagedResult);
            }
            catch (Exception ex)
            {
                return new APIResult(true, ResultMessage.ErrorTypes.GetData, "Error getting recent logs", ex.Message);
            }
        }

        public async Task<APIResult> GetErrorsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new SystemLogFilterRequest
                {
                    Level = "Error",
                    PageIndex = pageIndex,
                    PageSize = pageSize,
                    SortBy = "timestamp",
                    SortOrder = "desc"
                };

                var pagedResult = await _logRepository.SearchAsync(request, cancellationToken);
                return new APIResult(pagedResult);
            }
            catch (Exception ex)
            {
                return new APIResult(true, ResultMessage.ErrorTypes.GetData, "Error getting error logs", ex.Message);
            }
        }

        public async Task<APIResult> GetCriticalAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new SystemLogFilterRequest
                {
                    Level = "Critical",
                    PageIndex = pageIndex,
                    PageSize = pageSize,
                    SortBy = "timestamp",
                    SortOrder = "desc"
                };

                var pagedResult = await _logRepository.SearchAsync(request, cancellationToken);
                return new APIResult(pagedResult);
            }
            catch (Exception ex)
            {
                return new APIResult(true, ResultMessage.ErrorTypes.GetData, "Error getting critical logs", ex.Message);
            }
        }

        public async Task<APIResult> GetByCategoryAsync(string category, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new SystemLogFilterRequest
                {
                    Category = category,
                    PageIndex = pageIndex,
                    PageSize = pageSize,
                    SortBy = "timestamp",
                    SortOrder = "desc"
                };

                var pagedResult = await _logRepository.SearchAsync(request, cancellationToken);
                return new APIResult(pagedResult);
            }
            catch (Exception ex)
            {
                return new APIResult(true, ResultMessage.ErrorTypes.GetData, "Error getting logs by category", ex.Message);
            }
        }

        public async Task<APIResult> GetByLevelAsync(string level, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new SystemLogFilterRequest
                {
                    Level = level,
                    PageIndex = pageIndex,
                    PageSize = pageSize,
                    SortBy = "timestamp",
                    SortOrder = "desc"
                };

                var pagedResult = await _logRepository.SearchAsync(request, cancellationToken);
                return new APIResult(pagedResult);
            }
            catch (Exception ex)
            {
                return new APIResult(true, ResultMessage.ErrorTypes.GetData, "Error getting logs by level", ex.Message);
            }
        }

        public async Task<APIResult> DeleteOldLogsAsync(int daysOld, CancellationToken cancellationToken = default)
        {
            try
            {
                if (daysOld < 1)
                {
                    return new APIResult(true, ResultMessage.ErrorTypes.CheckData, "Invalid parameter", "daysOld must be greater than 0");
                }

                var result = await _logRepository.DeleteOldLogsAsync(daysOld, cancellationToken);
                if (result.IsError)
                {
                    return new APIResult(true, result.ErrorType, result.Message, result.MessageDetail);
                }

                return new APIResult(new { Message = result.MessageDetail });
            }
            catch (Exception ex)
            {
                return new APIResult(true, ResultMessage.ErrorTypes.Delete, "Error deleting old logs", ex.Message);
            }
        }

        public async Task<APIResult> GetStatsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var infoRequest = new SystemLogFilterRequest { Level = "Information", PageIndex = 1, PageSize = 1 };
                var warnRequest = new SystemLogFilterRequest { Level = "Warning", PageIndex = 1, PageSize = 1 };
                var errorRequest = new SystemLogFilterRequest { Level = "Error", PageIndex = 1, PageSize = 1 };
                var criticalRequest = new SystemLogFilterRequest { Level = "Critical", PageIndex = 1, PageSize = 1 };

                var infoResult = await _logRepository.SearchAsync(infoRequest, cancellationToken);
                var warnResult = await _logRepository.SearchAsync(warnRequest, cancellationToken);
                var errorResult = await _logRepository.SearchAsync(errorRequest, cancellationToken);
                var criticalResult = await _logRepository.SearchAsync(criticalRequest, cancellationToken);

                var stats = new
                {
                    TotalInformation = infoResult.Total,
                    TotalWarnings = warnResult.Total,
                    TotalErrors = errorResult.Total,
                    TotalCritical = criticalResult.Total,
                    GrandTotal = infoResult.Total + warnResult.Total + errorResult.Total + criticalResult.Total,
                    LastUpdate = DateTime.Now
                };

                return new APIResult(stats);
            }
            catch (Exception ex)
            {
                return new APIResult(true, ResultMessage.ErrorTypes.GetData, "Error getting statistics", ex.Message);
            }
        }
    }
}
