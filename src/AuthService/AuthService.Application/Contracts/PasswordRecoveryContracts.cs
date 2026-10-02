namespace AuthService.Application.Contracts
{
    /// <summary>PLAN.md mục 36 — luồng "Quên mật khẩu"/"Tài khoản bị khóa" dùng chung 1 cơ chế.</summary>
    public class ForgotPasswordRequest
    {
        public string Username { get; set; } = string.Empty;
        public string CaptchaToken { get; set; } = string.Empty;
    }

    public class VerifyResetOtpRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
    }

    /// <summary>ResetToken dùng 1 lần, không hiển thị cho user — client giữ để gọi bước reset (mục 36).</summary>
    public class VerifyResetOtpResponse
    {
        public string ResetToken { get; set; } = string.Empty;
    }

    public class ResetPasswordRequest
    {
        public string ResetToken { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>Trả otpauthUri mới — TotpSecret bị sinh lại toàn bộ sau reset (mục 36/42).</summary>
    public class ResetPasswordResponse
    {
        public string OtpauthUri { get; set; } = string.Empty;
    }
}
