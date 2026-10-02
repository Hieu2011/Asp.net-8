using AuthService.Application.Contracts;

namespace AuthService.Application.Interfaces
{
    /// <summary>Application Service — luồng "Quên mật khẩu"/"Tài khoản bị khóa" (PLAN.md mục 5b/36).</summary>
    public interface IPasswordRecoveryService
    {
        Task ForgotPasswordAsync(ForgotPasswordRequest request, string remoteIp, CancellationToken cancellationToken = default);

        Task<VerifyResetOtpResponse> VerifyResetOtpAsync(VerifyResetOtpRequest request, CancellationToken cancellationToken = default);

        Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request, string remoteIp, CancellationToken cancellationToken = default);
    }
}
