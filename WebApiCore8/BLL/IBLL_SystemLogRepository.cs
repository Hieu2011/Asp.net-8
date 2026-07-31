using ML;

namespace BLL
{
    public interface IBLL_SystemLogRepository
    {
        Task<PagedResult<SystemLog>> SearchAsync(SystemLogFilterRequest request);
        Task<(SystemLog?, ResultMessage)> GetLogByIDAsync(string id);
        Task<ResultMessage> DeleteOldLogsAsync(int daysOld);
    }
}