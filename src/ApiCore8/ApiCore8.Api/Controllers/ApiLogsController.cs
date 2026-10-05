using Shared.Common.Contracts;
using ApiCore8.Application.Contracts;
using ApiCore8.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ApiCore8.Api.Controllers
{
    /// <summary>
    /// Controller quản lý API execution logs ([LogApi] tự ghi vào MongoDB — xem ApiLoggingAttribute).
    /// Chỉ nhận request và gọi <see cref="IApiLogUseCase"/> — validate/orchestration nằm ở Application.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ApiLogsController : ControllerBase
    {
        private readonly IApiLogUseCase _apiLogUseCase;

        public ApiLogsController(IApiLogUseCase apiLogUseCase)
        {
            _apiLogUseCase = apiLogUseCase;
        }

        /// <summary>
        /// Search API logs bằng 1 từ khóa duy nhất — chỉ cần khớp (LIKE, không phân biệt hoa
        /// thường) BẤT KỲ 1 trong 3 field ApiName/RequestBody/ResponseBody là ra kết quả, kết hợp
        /// lọc theo khoảng ngày fromDate/toDate nếu có truyền.
        /// </summary>
        /// <param name="keyword">Từ khóa tìm trong ApiName hoặc RequestBody hoặc ResponseBody</param>
        /// <param name="fromDate">Lọc CreatedAt >= fromDate (tùy chọn) — PHẢI kèm offset múi giờ tường minh, VD: 2026-07-01T08:46:03Z hoặc 2026-07-01T15:46:03+07:00</param>
        /// <param name="toDate">Lọc CreatedAt &lt;= toDate (tùy chọn) — cùng định dạng với fromDate</param>
        /// <param name="page">Trang số (mặc định 1)</param>
        /// <param name="pageSize">Số dòng/trang (mặc định 20, tối đa 100)</param>
        [HttpGet("SearchStagingImagetest")]
        [ProducesResponseType(typeof(APIResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResult), StatusCodes.Status400BadRequest)]
        public Task<APIResult> Search(
            [FromQuery] string? keyword,
            [FromQuery] string? fromDate,
            [FromQuery] string? toDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
            => _apiLogUseCase.SearchAsync(keyword, fromDate, toDate, page, pageSize, cancellationToken);
    }
}
