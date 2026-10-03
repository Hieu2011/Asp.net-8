namespace AuthService.Application.Interfaces
{
    /// <summary>
    /// Bảng login_history — ghi mãi mãi mọi lần login thành công/thất bại (mục 4b), KHÔNG có
    /// endpoint đọc lại (chỉ audit) nên chỉ cần đúng 1 method Insert — không thêm method thừa
    /// chưa ai gọi (bài học từ IMongoData cũ, xem cleanup-log.md).
    /// </summary>
    public interface ILoginHistoryRepository
    {
        Task InsertAsync(
            Guid? userId,
            string usernameAttempted,
            string? ipAddress,
            string? deviceInfo,
            string loginType,
            bool isSuccess,
            string? failureReason,
            CancellationToken cancellationToken = default);
    }
}
