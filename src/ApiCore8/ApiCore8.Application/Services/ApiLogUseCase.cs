using Shared.Common.Contracts;
using ApiCore8.Application.Contracts;
using ApiCore8.Application.Interfaces;

namespace ApiCore8.Application.Services
{
    /// <inheritdoc cref="IApiLogUseCase" />
    public class ApiLogUseCase : IApiLogUseCase
    {
        private readonly IApiLogRepository _logRepository;

        public ApiLogUseCase(IApiLogRepository logRepository)
        {
            _logRepository = logRepository;
        }

        public async Task<APIResult> SearchAsync(
            string? keyword,
            string? fromDate,
            string? toDate,
            int page,
            int pageSize,
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
