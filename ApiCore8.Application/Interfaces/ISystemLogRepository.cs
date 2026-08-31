using ApiCore8.Application.Contracts;
using ApiCore8.Domain.Entities;

namespace ApiCore8.Application.Interfaces
{
    public interface ISystemLogRepository
    {
        Task<PagedResult<SystemLog>> SearchAsync(SystemLogFilterRequest request, CancellationToken cancellationToken = default);
        Task<(SystemLog?, ResultMessage)> GetLogByIDAsync(string id, CancellationToken cancellationToken = default);
        Task<ResultMessage> DeleteOldLogsAsync(int daysOld, CancellationToken cancellationToken = default);
        Task<ResultMessage> DeleteByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<ResultMessage> InsertAsync(SystemLog log, CancellationToken cancellationToken = default);
    }
}