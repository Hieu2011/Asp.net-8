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
        Task<RegisterResponse> RegisterAsync(RegisterRequest request, string remoteIp, CancellationToken cancellationToken = default);

        Task<TokenResponse> LoginAsync(LoginRequest request, string remoteIp, string? deviceInfo, CancellationToken cancellationToken = default);

        Task<TokenResponse> LoginWithPasswordAsync(LoginWithPasswordRequest request, string remoteIp, string? deviceInfo, CancellationToken cancellationToken = default);

        Task<TokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

        Task LogoutAsync(Guid sessionId, string actorUsername, CancellationToken cancellationToken = default);
    }
}
