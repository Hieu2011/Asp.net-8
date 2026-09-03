using ApiCore8.Application.Contracts;
using ApiCore8.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ApiCore8.Api.Controllers
{
    /// <summary>
    /// Controller quản lý API execution logs ([LogApi] tự ghi vào MongoDB — xem ApiLoggingAttribute).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ApiLogsController : ControllerBase
    {
        private readonly IApiLogRepository _logRepository;

        public ApiLogsController(IApiLogRepository logRepository)
        {
            _logRepository = logRepository;
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
        [HttpGet("SearchLog")]
        [ProducesResponseType(typeof(APIResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResult), StatusCodes.Status400BadRequest)]
        public async Task<APIResult> Search(
            [FromQuery] string? keyword,
            [FromQuery] string? fromDate,
            [FromQuery] string? toDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            // Nhận string thay vì để model binder tự parse thẳng DateTime — nếu thiếu offset,
            // binder mặc định sẽ ÂM THẦM tự điền offset theo giờ server (không throw), gây sai
            // lệch so với CreatedAt (UTC) trong Mongo, filter Gte/Lte match sai/rỗng.
            DateTime? fromDateUtc = null;
            if (!string.IsNullOrWhiteSpace(fromDate))
            {
                if (!ExplicitOffsetDateTimeParser.TryParse(fromDate, out var fromDateOffset))
                {
                    return new APIResult(true, ResultMessage.ErrorTypes.Validation,
                        "fromDate phải kèm offset múi giờ tường minh, VD: 2026-07-01T08:46:03Z hoặc 2026-07-01T15:46:03+07:00", string.Empty);
                }
                fromDateUtc = fromDateOffset.UtcDateTime;
            }

            DateTime? toDateUtc = null;
            if (!string.IsNullOrWhiteSpace(toDate))
            {
                if (!ExplicitOffsetDateTimeParser.TryParse(toDate, out var toDateOffset))
                {
                    return new APIResult(true, ResultMessage.ErrorTypes.Validation,
                        "toDate phải kèm offset múi giờ tường minh, VD: 2026-07-01T23:59:59Z hoặc 2026-07-01T23:59:59+07:00", string.Empty);
                }
                toDateUtc = toDateOffset.UtcDateTime;
            }

            try
            {
                var request = new ApiLogKeywordSearchRequest
                {
                    Keyword = keyword,
                    FromDate = fromDateUtc,
                    ToDate = toDateUtc,
                    Page = page < 1 ? 1 : page,
                    PageSize = pageSize < 1 || pageSize > 100 ? 20 : pageSize
                };

                var pagedResult = await _logRepository.SearchByKeywordAsync(request, cancellationToken);
                return new APIResult(pagedResult);
            }
            catch (Exception ex)
            {
                return new APIResult(true, ResultMessage.ErrorTypes.SearchData,
                    "Error searching API logs", ex.Message);
            }
        }
    }
}
