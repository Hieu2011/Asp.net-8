using AuthService.Application.Contracts;

namespace AuthService.Application.Interfaces
{
    /// <summary>Application Service — luồng Google Sign-In (PLAN.md mục 5b/45/46).</summary>
    public interface IGoogleAuthService
    {
        Task<GoogleLoginResponse> LoginWithGoogleAsync(GoogleLoginRequest request, CancellationToken cancellationToken = default);

        Task<GoogleCompleteSetupResponse> CompleteSetupAsync(GoogleCompleteSetupRequest request, string remoteIp, CancellationToken cancellationToken = default);
    }
}
