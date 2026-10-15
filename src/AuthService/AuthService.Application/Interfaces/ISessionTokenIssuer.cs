using AuthService.Application.Contracts;
using AuthService.Domain;

namespace AuthService.Application.Interfaces
{
    /// <summary>Tạo session mới + cấp access/refresh token — dùng chung cho đăng nhập OTP, mật khẩu, QR.</summary>
    public interface ISessionTokenIssuer
    {
        /// <param name="loginMethod">Hằng số trong <see cref="LoginMethods"/> — quyết định email cảnh báo gửi cho chủ tài khoản.</param>
        Task<TokenResponse> IssueAsync(User user, string remoteIp, string? deviceInfo, string loginMethod, CancellationToken cancellationToken = default);
    }
}
