namespace AuthService.Application.Contracts
{
    /// <summary>POST /auth/register — bước 1: kiểm tra + gửi mã OTP về email, CHƯA tạo tài khoản (AUTH-09).</summary>
    public class RegisterRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string CaptchaToken { get; set; } = string.Empty;
    }

    /// <summary>Kết quả gửi mã (đăng ký/gửi lại) — client giữ RegisterToken để xác nhận.</summary>
    public class RegisterOtpSentResponse
    {
        public string RegisterToken { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        /// <summary>Số giây mã còn hiệu lực.</summary>
        public int ExpiresIn { get; set; }
        /// <summary>Số giây phải chờ trước khi được gửi lại mã.</summary>
        public int ResendAfter { get; set; }
    }

    /// <summary>POST /auth/register/verify — bước 2: nhập đúng mã mới tạo tài khoản.</summary>
    public class RegisterVerifyRequest
    {
        public string RegisterToken { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
    }

    /// <summary>POST /auth/register/resend.</summary>
    public class RegisterResendRequest
    {
        public string RegisterToken { get; set; } = string.Empty;
    }

    /// <summary>Trả otpauth:// URI để user nhập tay/quét QR vào Google Authenticator (mục 2 quyết định).</summary>
    public class RegisterResponse
    {
        public string OtpauthUri { get; set; } = string.Empty;
    }
}
