using AuthService.Application.Contracts;

namespace AuthService.Application.Interfaces
{
    /// <summary>
    /// Application Service (Use Case) — orchestration cho luồng đăng ký/đăng nhập chính (PLAN.md mục 5b).
    /// Implement (<c>AuthenticationService</c>) nằm ngay trong Application, không phải Infrastructure —
    /// không tự làm I/O, chỉ gọi lại các interface repository/adapter đã có.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>Bước 1 đăng ký: kiểm tra + gửi mã OTP về email, chưa tạo tài khoản (AUTH-09).</summary>
        Task<RegisterOtpSentResponse> RegisterAsync(RegisterRequest request, string remoteIp, CancellationToken cancellationToken = default);

        /// <summary>Bước 2: đúng mã → tạo tài khoản, trả otpauth URI để quét QR.</summary>
        Task<RegisterResponse> VerifyRegisterAsync(RegisterVerifyRequest request, CancellationToken cancellationToken = default);

        Task<RegisterOtpSentResponse> ResendRegisterOtpAsync(RegisterResendRequest request, CancellationToken cancellationToken = default);

        Task<TokenResponse> LoginAsync(LoginRequest request, string remoteIp, string? deviceInfo, CancellationToken cancellationToken = default);

        Task<TokenResponse> LoginWithPasswordAsync(LoginWithPasswordRequest request, string remoteIp, string? deviceInfo, CancellationToken cancellationToken = default);

        Task<TokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

        Task LogoutAsync(Guid sessionId, string actorUsername, CancellationToken cancellationToken = default);
    }
}
