using Shared.Common.Contracts;
using ApiCore8.Application.Contracts;

namespace ApiCore8.Application.Interfaces
{
    /// <summary>
    /// Application Service (Use Case) — điều phối <see cref="ISystemLogRepository"/> + validate/chuẩn hóa
    /// input, trả thẳng <see cref="APIResult"/> để Controller chỉ còn việc gọi và return.
    /// Không tự làm I/O (không tự mở connection Mongo) — chỉ gọi lại repository đã inject.
    /// </summary>
    public interface ISystemLogUseCase
    {
        Task<APIResult> InsertAsync(InsertSystemLogRequest request, CancellationToken cancellationToken = default);
        Task<APIResult> DeleteByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<APIResult> SearchAsync(SystemLogFilterRequest request, CancellationToken cancellationToken = default);
        Task<APIResult> GetLogByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<APIResult> GetRecentAsync(int limit, CancellationToken cancellationToken = default);
        Task<APIResult> GetErrorsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default);
        Task<APIResult> GetCriticalAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default);
        Task<APIResult> GetByCategoryAsync(string category, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
        Task<APIResult> GetByLevelAsync(string level, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
        Task<APIResult> DeleteOldLogsAsync(int daysOld, CancellationToken cancellationToken = default);
        Task<APIResult> GetStatsAsync(CancellationToken cancellationToken = default);
    }
}
