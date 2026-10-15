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

        /// <summary>Public key (PEM) của các khóa cũ còn chấp nhận khi xoay khóa — giữ tới khi token ký bằng khóa cũ hết hạn rồi xóa.</summary>
        public string[] RsaPreviousPublicKeys { get; set; } = [];

        /// <summary>Claim <c>iss</c> của mọi token AuthService phát hành.</summary>
        public string Issuer { get; set; } = "hpm-auth";

        /// <summary>Claim <c>aud</c> của token người dùng (web/BFF) — AuthService chỉ nhận audience này.</summary>
        public string UserAudience { get; set; } = "hpm-web";

        /// <summary>Claim <c>aud</c> của token đối tác Flow B (M2M).</summary>
        public string PartnerAudience { get; set; } = "hpm-partner";

        /// <summary>Refresh token cũ dùng lại trong khoảng này sau lần xoay = request song song (2 tab), không coi là bị đánh cắp.</summary>
        public TimeSpan RefreshReuseGraceWindow { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>Thời gian khóa riêng đường OTP sau 3 lần sai (mục 17). Mặc định 1 tiếng.</summary>
        public TimeSpan OtpLockoutDuration { get; set; } = TimeSpan.FromHours(1);

        /// <summary>Thời gian khóa toàn bộ tài khoản khi Password fallback cũng sai quá ngưỡng (mục 18). Mặc định 1 tiếng.</summary>
        public TimeSpan AccountLockoutDuration { get; set; } = TimeSpan.FromHours(1);

        /// <summary>TTL mã OTP gửi qua email lúc quên mật khẩu (mục 36). Mặc định 10 phút.</summary>
        public TimeSpan PasswordResetOtpTtl { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>TTL resetToken sinh ra sau khi verify OTP đúng (mục 36). Mặc định 15 phút.</summary>
        public TimeSpan PasswordResetTokenTtl { get; set; } = TimeSpan.FromMinutes(15);

        /// <summary>TTL mã OTP xác thực email khi đăng ký + bản đăng ký chờ trong Redis (AUTH-09). Mặc định 10 phút.</summary>
        public TimeSpan RegisterOtpTtl { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>Thời gian chờ giữa 2 lần gửi mã đăng ký cho cùng email/bản chờ (AUTH-09). Mặc định 60 giây.</summary>
        public TimeSpan RegisterOtpResendCooldown { get; set; } = TimeSpan.FromSeconds(60);

        /// <summary>Thời gian sống của 1 mã QR đăng nhập (AUTH-10). Mặc định 2 phút.</summary>
        public TimeSpan QrLoginTtl { get; set; } = TimeSpan.FromMinutes(2);

        /// <summary>TTL setupToken cho user mới đăng ký qua Google (mục 47). Mặc định 15 phút.</summary>
        public TimeSpan GoogleSetupTokenTtl { get; set; } = TimeSpan.FromMinutes(15);

        /// <summary>TTL access token JWT (mục 11). Mặc định 5 phút.</summary>
        public TimeSpan AccessTokenTtl { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>Session.ExpiresAt — giới hạn tuyệt đối, refresh rotation không được gia hạn quá mốc này (mục 21). Mặc định 12 tiếng.</summary>
        public TimeSpan SessionAbsoluteTtl { get; set; } = TimeSpan.FromHours(12);

        /// <summary>TTL cache GET /auth/me (mục 5b phát sinh) — tự hết hạn, không cần invalidate mọi nơi. Mặc định 5 phút.</summary>
        public TimeSpan UserProfileCacheTtl { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>IP/CIDR proxy nội bộ được tin header X-Forwarded-For (VD server Next.js BFF, Docker network). Rỗng = chỉ tin Cloudflare.</summary>
        public string[] TrustedProxies { get; set; } = [];
    }
}
