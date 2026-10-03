using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.Options
{
    /// <summary>
    /// Bind từ section "Auth" (appsettings/User Secrets/env). Options pattern (PLAN.md mục 49) —
    /// <c>ValidateDataAnnotations()</c> + <c>ValidateOnStart()</c> fail ngay lúc start-up nếu thiếu
    /// key bắt buộc, không chờ tới lúc có request mới lộ ra lỗi.
    /// </summary>
    public class AuthOptions
    {
        public const string SectionName = "Auth";

        /// <summary>AES key mã hóa TotpSecret trước khi lưu DB (mục 13).</summary>
        [Required]
        public string TotpEncryptionKey { get; set; } = string.Empty;

        /// <summary>RSA private key (PEM) dùng ký JWT (mục 4/16/43).</summary>
        [Required]
        public string RsaPrivateKey { get; set; } = string.Empty;

        /// <summary>Thời gian khóa riêng đường OTP sau 3 lần sai (mục 17). Mặc định 1 tiếng.</summary>
        public TimeSpan OtpLockoutDuration { get; set; } = TimeSpan.FromHours(1);

        /// <summary>Thời gian khóa toàn bộ tài khoản khi Password fallback cũng sai quá ngưỡng (mục 18). Mặc định 1 tiếng.</summary>
        public TimeSpan AccountLockoutDuration { get; set; } = TimeSpan.FromHours(1);

        /// <summary>TTL mã OTP gửi qua email lúc quên mật khẩu (mục 36). Mặc định 10 phút.</summary>
        public TimeSpan PasswordResetOtpTtl { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>TTL resetToken sinh ra sau khi verify OTP đúng (mục 36). Mặc định 15 phút.</summary>
        public TimeSpan PasswordResetTokenTtl { get; set; } = TimeSpan.FromMinutes(15);

        /// <summary>TTL setupToken cho user mới đăng ký qua Google (mục 47). Mặc định 15 phút.</summary>
        public TimeSpan GoogleSetupTokenTtl { get; set; } = TimeSpan.FromMinutes(15);

        /// <summary>TTL access token JWT (mục 11). Mặc định 5 phút.</summary>
        public TimeSpan AccessTokenTtl { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>Session.ExpiresAt — giới hạn tuyệt đối, refresh rotation không được gia hạn quá mốc này (mục 21). Mặc định 12 tiếng.</summary>
        public TimeSpan SessionAbsoluteTtl { get; set; } = TimeSpan.FromHours(12);

        /// <summary>TTL cache GET /auth/me (mục 5b phát sinh) — tự hết hạn, không cần invalidate mọi nơi. Mặc định 5 phút.</summary>
        public TimeSpan UserProfileCacheTtl { get; set; } = TimeSpan.FromMinutes(5);
    }
}
