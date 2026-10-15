using AuthService.Domain;

namespace AuthService.Application.Interfaces
{
    /// <summary>Email cảnh báo bảo mật cho chủ tài khoản. Gửi lỗi chỉ ghi log — không làm hỏng luồng đăng nhập/đặt lại mật khẩu.</summary>
    public interface ISecurityNotifier
    {
        /// <summary>Đăng nhập bằng QR → luôn báo; cách khác → chỉ báo khi thiết bị (User-Agent) chưa từng đăng nhập.</summary>
        Task NotifyLoginAsync(User user, string remoteIp, string? deviceInfo, string loginMethod, CancellationToken cancellationToken = default);

        Task NotifyPasswordResetAsync(User user, string remoteIp, CancellationToken cancellationToken = default);
    }
}
