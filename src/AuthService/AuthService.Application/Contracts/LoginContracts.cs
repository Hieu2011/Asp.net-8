namespace AuthService.Application.Contracts
{
    /// <summary>PLAN.md mục 7 — POST /auth/login (luồng chính, Username + OTP). CaptchaToken chỉ bắt
    /// buộc khi otp_fail_count >= 1 (mục 44), nên để nullable — service tự quyết định có bắt buộc không.</summary>
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
        public string? CaptchaToken { get; set; }
    }

    /// <summary>POST /auth/login/password — fallback sau 3 lần sai OTP (mục 29), luôn bắt buộc captcha.</summary>
    public class LoginWithPasswordRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string CaptchaToken { get; set; } = string.Empty;
    }

    /// <summary>Kết quả issue JWT thành công (mục 9 — access token 5 phút + refresh token).</summary>
    public class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
    }

    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}
