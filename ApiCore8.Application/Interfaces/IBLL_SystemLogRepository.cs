using ApiCore8.Application.Contracts;
using ApiCore8.Domain.Entities;

namespace ApiCore8.Application.Interfaces
{
    public interface IBLL_SystemLogRepository
    {
        Task<PagedResult<SystemLog>> SearchAsync(SystemLogFilterRequest request);
        Task<(SystemLog?, ResultMessage)> GetLogByIDAsync(string id);
        Task<ResultMessage> DeleteOldLogsAsync(int daysOld);
    }
}