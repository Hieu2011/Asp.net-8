using Shared.Common.Contracts;
using ApiCore8.Application.Contracts;
using ApiCore8.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ApiCore8.Api.Controllers
{
    /// <summary>
    /// Controller quản lý System Logs (logs từ ILogger + MongoDB). Chỉ nhận request và gọi
    /// <see cref="ISystemLogUseCase"/> — toàn bộ validate/orchestration nằm ở tầng Application
    /// (Application Service / Use Case pattern), không xử lý logic ở Controller.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SystemLogsController : ControllerBase
    {
        private readonly ISystemLogUseCase _systemLogUseCase;

        public SystemLogsController(ISystemLogUseCase systemLogUseCase)
        {
            _systemLogUseCase = systemLogUseCase;
        }

        /// <summary>
        /// Thêm 1 log test thủ công (không cần đợi app tự sinh log thật qua Serilog).
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(APIResult), StatusCodes.Status200OK)]
        public Task<APIResult> Insert([FromBody] InsertSystemLogRequest request, CancellationToken cancellationToken)
            => _systemLogUseCase.InsertAsync(request, cancellationToken);

        /// <summary>
        /// Xóa 1 log theo đúng ID (khác DeleteOld — xóa hàng loạt theo số ngày).
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(APIResult), StatusCodes.Status200OK)]
        public Task<APIResult> DeleteById(string id, CancellationToken cancellationToken)
            => _systemLogUseCase.DeleteByIdAsync(id, cancellationToken);

        /// <summary>
        /// Search system logs với filter + pagination
        /// </summary>
        /// <param name="request">Filter request với Level, Category, Message, Date range, etc.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Paged result với danh sách SystemLog</returns>
        /// <response code="200">Returns paged system logs</response>
        /// <response code="400">Invalid request parameters</response>
        [HttpPost("Search")]
        [ProducesResponseType(typeof(APIResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResult), StatusCodes.Status400BadRequest)]
        public Task<APIResult> Search([FromBody] SystemLogFilterRequest request, CancellationToken cancellationToken)
            => _systemLogUseCase.SearchAsync(request, cancellationToken);

        /// <summary>
        /// Get system log by ID
        /// </summary>
        /// <param name="request">Request chứa ID (ObjectId string)</param>
        /// <param name="cancellationToken"></param>
        /// <returns>SystemLog entity</returns>
        /// <response code="200">Returns the system log</response>
        /// <response code="404">Log not found</response>
        [HttpPost("GetLogByID")]
        [ProducesResponseType(typeof(APIResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResult), StatusCodes.Status404NotFound)]
        public Task<APIResult> GetLogByID([FromBody] SystemLogFilterRequest request, CancellationToken cancellationToken)
            => _systemLogUseCase.GetLogByIdAsync(request?.Id ?? string.Empty, cancellationToken);

        /// <summary>
        /// Get recent system logs (quick access)
        /// </summary>
        /// <param name="limit">Number of logs to return (default: 50, max: 100)</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Recent system logs</returns>
        [HttpGet("Recent")]
        [ProducesResponseType(typeof(APIResult), StatusCodes.Status200OK)]
        public Task<APIResult> GetRecent([FromQuery] int limit = 50, CancellationToken cancellationToken = default)
            => _systemLogUseCase.GetRecentAsync(limit, cancellationToken);

        /// <summary>
        /// Get error logs only (Level = Error hoặc Critical)
        /// </summary>
        /// <param name="pageIndex">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 20)</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Paged error logs</returns>
        [HttpGet("Errors")]
        [ProducesResponseType(typeof(APIResult), StatusCodes.Status200OK)]
        public Task<APIResult> GetErrors([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
            => _systemLogUseCase.GetErrorsAsync(pageIndex, pageSize, cancellationToken);

        /// <summary>
        /// Get critical logs only
        /// </summary>
        /// <param name="pageIndex">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Paged critical logs</returns>
        [HttpGet("Critical")]
        [ProducesResponseType(typeof(APIResult), StatusCodes.Status200OK)]
        public Task<APIResult> GetCritical([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
            => _systemLogUseCase.GetCriticalAsync(pageIndex, pageSize, cancellationToken);

        /// <summary>
        /// Get logs by category (e.g., "RedisConnectionService", "MongoData", "ApiLogRepository")
        /// </summary>
        /// <param name="category">Category name (partial match supported)</param>
        /// <param name="pageIndex">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Paged logs for specific category</returns>
        [HttpGet("Category/{category}")]
        [ProducesResponseType(typeof(APIResult), StatusCodes.Status200OK)]
        public Task<APIResult> GetByCategory(
            string category,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
            => _systemLogUseCase.GetByCategoryAsync(category, pageIndex, pageSize, cancellationToken);

        /// <summary>
        /// Get logs by level (Information, Warning, Error, Critical, Debug)
        /// </summary>
        /// <param name="level">Log level</param>
        /// <param name="pageIndex">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Paged logs for specific level</returns>
        [HttpGet("Level/{level}")]
        [ProducesResponseType(typeof(APIResult), StatusCodes.Status200OK)]
        public Task<APIResult> GetByLevel(
            string level,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
            => _systemLogUseCase.GetByLevelAsync(level, pageIndex, pageSize, cancellationToken);

        /// <summary>
        /// Delete old system logs (cleanup)
        /// </summary>
        /// <param name="daysOld">Xóa logs cũ hơn số ngày này (default: 30)</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Result với số logs đã xóa</returns>
        /// <response code="200">Logs deleted successfully</response>
        [HttpDelete("DeleteOld")]
        [ProducesResponseType(typeof(APIResult), StatusCodes.Status200OK)]
        public Task<APIResult> DeleteOldLogs([FromQuery] int daysOld = 30, CancellationToken cancellationToken = default)
            => _systemLogUseCase.DeleteOldLogsAsync(daysOld, cancellationToken);

        /// <summary>
        /// Get log statistics (counts by level, recent errors, etc.)
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Statistics object</returns>
        [HttpGet("Stats")]
        [ProducesResponseType(typeof(APIResult), StatusCodes.Status200OK)]
        public Task<APIResult> GetStats(CancellationToken cancellationToken)
            => _systemLogUseCase.GetStatsAsync(cancellationToken);
    }
}
