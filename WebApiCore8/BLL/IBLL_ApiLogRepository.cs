using ML;

namespace BLL
{
    /// <summary>
    /// Interface cho ApiExecutionLog repository
    /// CHỈ ĐỊNH NGHĨA CÁC METHODS THỰC TẾ SỬ DỤNG
    /// </summary>
    public interface IBLL_ApiLogRepository
    {
        /// <summary>
        /// Insert log vào MongoDB
        /// </summary>
        /// <param name="log">ApiExecutionLog entity</param>
        /// <returns>true nếu insert thành công</returns>
        Task<bool> InsertLog(ApiExecutionLog log);
        
        /// <summary>
        /// Search logs với filter + pagination
        /// Sử dụng LogFilterRequest với các fields: Id, ApiName, Method, From, To, Keyword, Page, PageSize
        /// </summary>
        /// <param name="request">LogFilterRequest</param>
        /// <returns>PagedResult với danh sách ApiExecutionLog</returns>
        Task<PagedResult<ApiExecutionLog>> Search(LogFilterRequest request);
        
        /// <summary>
        /// Lấy log theo ID
        /// </summary>
        /// <param name="id">MongoDB ObjectId string</param>
        /// <returns>Tuple (ApiExecutionLog, ResultMessage)</returns>
        Task<(ApiExecutionLog?, ResultMessage)> GetLogByID(string id);
        
        /// <summary>
        /// Lấy slow queries (ExecutionMs > threshold)
        /// </summary>
        /// <param name="thresholdMs">Ngưỡng thời gian (milliseconds)</param>
        /// <param name="pageIndex">Trang số (bắt đầu từ 1)</param>
        /// <param name="pageSize">Số items mỗi trang</param>
        /// <returns>PagedResult với các slow logs</returns>
        Task<PagedResult<ApiExecutionLog>> GetSlowLogs(long thresholdMs = 1000, int pageIndex = 1, int pageSize = 20);
        
        /// <summary>
        /// Xóa logs cũ hơn số ngày chỉ định
        /// </summary>
        /// <param name="daysOld">Số ngày (logs cũ hơn sẽ bị xóa)</param>
        /// <returns>ResultMessage với số lượng đã xóa</returns>
        Task<ResultMessage> DeleteOldLogs(int daysOld);
    }
}