namespace AuthService.Application.Contracts
{
    /// <summary>PLAN.md mục 7 — POST /auth/register. Bắt buộc email (mục 38), password policy (mục 27), captcha (mục 44).</summary>
    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string CaptchaToken { get; set; } = string.Empty;
    }

    /// <summary>Trả otpauth:// URI để user nhập tay/quét QR vào Google Authenticator (mục 2 quyết định).</summary>
    public class RegisterResponse
    {
        public string OtpauthUri { get; set; } = string.Empty;
    }
}
