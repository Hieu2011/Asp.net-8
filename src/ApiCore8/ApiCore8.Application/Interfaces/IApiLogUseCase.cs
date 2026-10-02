using Shared.Common.Contracts;
using ApiCore8.Application.Contracts;

namespace ApiCore8.Application.Interfaces
{
    /// <summary>
    /// Application Service (Use Case) — điều phối <see cref="IApiLogRepository"/> + validate/chuẩn hóa
    /// input (parse ngày có offset tường minh, clamp page/pageSize), trả thẳng <see cref="APIResult"/>.
    /// </summary>
    public interface IApiLogUseCase
    {
        Task<APIResult> SearchAsync(
            string? keyword,
            string? fromDate,
            string? toDate,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);
    }
}
