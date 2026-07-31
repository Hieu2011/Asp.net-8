using Microsoft.AspNetCore.Mvc;
using ML;
using BLL;
using MongoDB.Bson;

namespace ApiCore8.Controllers
{
    /// <summary>
    /// Controller quản lý System Logs (logs từ ILogger + MongoDB)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SystemLogsController : ControllerBase
    {
        private readonly IBLL_SystemLogRepository _logRepository;

        public SystemLogsController(IBLL_SystemLogRepository logRepository)
        {
            _logRepository = logRepository;
        }

        /// <summary>
        /// Search system logs với filter + pagination
        /// </summary>
        /// <param name="request">Filter request với Level, Category, Message, Date range, etc.</param>
        /// <returns>Paged result với danh sách SystemLog</returns>
        /// <response code="200">Returns paged system logs</response>
        /// <response code="400">Invalid request parameters</response>
        [HttpPost("Search")]
        [ProducesResponseType(typeof(APIResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResult), StatusCodes.Status400BadRequest)]
        public async Task<APIResult> Search([FromBody] SystemLogFilterRequest request)
        {
            try
            {
                // Validate request
                if (request == null)
                {
                    return new APIResult(true, ResultMessage.ErrorTypes.CheckData,
                        "Invalid request", "Request body cannot be null");
                }

                // Validate pagination
                if (request.PageIndex < 1)
                    request.PageIndex = 1;

                if (request.PageSize < 1 || request.PageSize > 100)
                    request.PageSize = 20;

                // Execute search
                var pagedResult = await _logRepository.SearchAsync(request);
                
                return new APIResult(pagedResult);
            }
            catch (Exception ex)
            {
                return new APIResult(true, ResultMessage.ErrorTypes.SearchData,
                    "Error searching system logs", ex.Message);
            }
        }

        /// <summary>
        /// Get system log by ID
        /// </summary>
        /// <param name="request">Request chứa ID (ObjectId string)</param>
        /// <returns>SystemLog entity</returns>
        /// <response code="200">Returns the system log</response>
        /// <response code="404">Log not found</response>
        [HttpPost("GetLogByID")]
        [ProducesResponseType(typeof(APIResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResult), StatusCodes.Status404NotFound)]
        public async Task<APIResult> GetLogByID([FromBody] SystemLogFilterRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.Id))
                {
                    return new APIResult(true, ResultMessage.ErrorTypes.GetData,
                        "Invalid request", "Id is required");
                }

                if (!ObjectId.TryParse(request.Id, out var objectId))
                {
                    return new APIResult(true, ResultMessage.ErrorTypes.GetData,
                        "Invalid request", "Id is not a valid ObjectId");
                }

                var (log, resultMessage) = await _logRepository.GetLogByIDAsync(request.Id);

                if (resultMessage.IsError)
                {
                    return new APIResult(true, resultMessage.ErrorType,
                        resultMessage.Message, resultMessage.MessageDetail);
                }

                return new APIResult(log);
            }
            catch (Exception ex)
            {
                return new APIResult(true, ResultMessage.ErrorTypes.GetData,
                    "Error getting system log", ex.Message);
            }
        }

        /// <summary>
        /// Get recent system logs (quick access)
        /// </summary>
        /// <param name="limit">Number of logs to return (default: 50, max: 100)</param>
        /// <returns>Recent system logs</returns>
        [HttpGet("Recent")]
        [ProducesResponseType(typeof(APIResult), StatusCodes.Status200OK)]
        public async Task<APIResult> GetRecent([FromQuery] int limit = 50)
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

                var pagedResult = await _logRepository.SearchAsync(request);
                return new APIResult(pagedResult);
            }
            catch (Exception ex)
            {
                return new APIResult(true, ResultMessage.ErrorTypes.GetData,
                    "Error getting recent logs", ex.Message);
            }
        }

        /// <summary>
        /// Get error logs only (Level = Error hoặc Critical)
        /// </summary>
        /// <param name="pageIndex">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 20)</param>
        /// <returns>Paged error logs</returns>
        [HttpGet("Errors")]
        [ProducesResponseType(typeof(APIResult), StatusCodes.Status200OK)]
        public async Task<APIResult> GetErrors([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
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

                var pagedResult = await _logRepository.SearchAsync(request);
                return new APIResult(pagedResult);
            }
            catch (Exception ex)
            {
                return new APIResult(true, ResultMessage.ErrorTypes.GetData,
                    "Error getting error logs", ex.Message);
            }
        }

        /// <summary>
        /// Get critical logs only
        /// </summary>
        /// <param name="pageIndex">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Paged critical logs</returns>
        [HttpGet("Critical")]
        [ProducesResponseType(typeof(APIResult), StatusCodes.Status200OK)]
        public async Task<APIResult> GetCritical([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
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

                var pagedResult = await _logRepository.SearchAsync(request);
                return new APIResult(pagedResult);
            }
            catch (Exception ex)
            {
                return new APIResult(true, ResultMessage.ErrorTypes.GetData,
                    "Error getting critical logs", ex.Message);
            }
        }

        /// <summary>
        /// Get logs by category (e.g., "RedisConnectionService", "MongoData", "BLL_ApiLogRepository")
        /// </summary>
        /// <param name="category">Category name (partial match supported)</param>
        /// <param name="pageIndex">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Paged logs for specific category</returns>
        [HttpGet("Category/{category}")]
        [ProducesResponseType(typeof(APIResult), StatusCodes.Status200OK)]
        public async Task<APIResult> GetByCategory(
            string category, 
            [FromQuery] int pageIndex = 1, 
            [FromQuery] int pageSize = 20)
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

                var pagedResult = await _logRepository.SearchAsync(request);
                return new APIResult(pagedResult);
            }
            catch (Exception ex)
            {
                return new APIResult(true, ResultMessage.ErrorTypes.GetData,
                    "Error getting logs by category", ex.Message);
            }
        }

        /// <summary>
        /// Get logs by level (Information, Warning, Error, Critical, Debug)
        /// </summary>
        /// <param name="level">Log level</param>
        /// <param name="pageIndex">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Paged logs for specific level</returns>
        [HttpGet("Level/{level}")]
        [ProducesResponseType(typeof(APIResult), StatusCodes.Status200OK)]
        public async Task<APIResult> GetByLevel(
            string level, 
            [FromQuery] int pageIndex = 1, 
            [FromQuery] int pageSize = 20)
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

                var pagedResult = await _logRepository.SearchAsync(request);
                return new APIResult(pagedResult);
            }
            catch (Exception ex)
            {
                return new APIResult(true, ResultMessage.ErrorTypes.GetData,
                    "Error getting logs by level", ex.Message);
            }
        }

        /// <summary>
        /// Delete old system logs (cleanup)
        /// </summary>
        /// <param name="daysOld">Xóa logs cũ hơn số ngày này (default: 30)</param>
        /// <returns>Result với số logs đã xóa</returns>
        /// <response code="200">Logs deleted successfully</response>
        [HttpDelete("DeleteOld")]
        [ProducesResponseType(typeof(APIResult), StatusCodes.Status200OK)]
        public async Task<APIResult> DeleteOldLogs([FromQuery] int daysOld = 30)
        {
            try
            {
                if (daysOld < 1)
                {
                    return new APIResult(true, ResultMessage.ErrorTypes.CheckData,
                        "Invalid parameter", "daysOld must be greater than 0");
                }

                var result = await _logRepository.DeleteOldLogsAsync(daysOld);

                if (result.IsError)
                {
                    return new APIResult(true, result.ErrorType,
                        result.Message, result.MessageDetail);
                }

                return new APIResult(new { Message = result.MessageDetail });
            }
            catch (Exception ex)
            {
                return new APIResult(true, ResultMessage.ErrorTypes.Delete,
                    "Error deleting old logs", ex.Message);
            }
        } 

        /// <summary>
        /// Get log statistics (counts by level, recent errors, etc.)
        /// </summary>
        /// <returns>Statistics object</returns>
        [HttpGet("Stats")]
        [ProducesResponseType(typeof(APIResult), StatusCodes.Status200OK)]
        public async Task<APIResult> GetStats()
        {
            try
            {
                // Get counts for each level
                var infoRequest = new SystemLogFilterRequest { Level = "Information", PageIndex = 1, PageSize = 1 };
                var warnRequest = new SystemLogFilterRequest { Level = "Warning", PageIndex = 1, PageSize = 1 };
                var errorRequest = new SystemLogFilterRequest { Level = "Error", PageIndex = 1, PageSize = 1 };
                var criticalRequest = new SystemLogFilterRequest { Level = "Critical", PageIndex = 1, PageSize = 1 };

                var infoResult = await _logRepository.SearchAsync(infoRequest);
                var warnResult = await _logRepository.SearchAsync(warnRequest);
                var errorResult = await _logRepository.SearchAsync(errorRequest);
                var criticalResult = await _logRepository.SearchAsync(criticalRequest);

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
                return new APIResult(true, ResultMessage.ErrorTypes.GetData,
                    "Error getting statistics", ex.Message);
            }
        }
    }
}