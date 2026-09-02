using ApiCore8.Application.Contracts;
using ApiCore8.Domain.Entities;

namespace ApiCore8.Application.Interfaces
{
    /// <summary>
    /// Interface cho ApiExecutionLog repository
    /// CHỈ ĐỊNH NGHĨA CÁC METHODS THỰC TẾ SỬ DỤNG
    /// </summary>
    public interface IApiLogRepository
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
        /// Search 1 từ khóa duy nhất — khớp kiểu LIKE (regex, không phân biệt hoa thường) trên
        /// BẤT KỲ field nào trong ApiName/RequestBody/ResponseBody (OR), kết hợp AND với
        /// FromDate (lọc theo StartTime) / ToDate (lọc theo EndTime).
        /// </summary>
        /// <param name="request">ApiLogKeywordSearchRequest</param>
        /// <param name="cancellationToken"></param>
        /// <returns>PagedResult với danh sách ApiExecutionLog khớp bất kỳ field nào</returns>
        Task<PagedResult<ApiExecutionLog>> SearchByKeywordAsync(ApiLogKeywordSearchRequest request, CancellationToken cancellationToken = default);

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