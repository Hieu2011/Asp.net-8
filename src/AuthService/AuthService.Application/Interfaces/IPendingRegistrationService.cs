namespace AuthService.Application.Interfaces
{
    /// <summary>Thông tin đăng ký đã qua kiểm tra, chờ xác thực email (mật khẩu đã hash).</summary>
    public class PendingRegistration
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
    }

    /// <summary>Redis-backed — đăng ký chờ nhập mã OTP gửi qua email (AUTH-09). Chưa ghi DB cho tới khi xác thực xong.</summary>
    public interface IPendingRegistrationService
    {
        /// <summary>Còn bao lâu mới được gửi mã tiếp cho email này (chống spam hộp thư); null = gửi được.</summary>
        Task<TimeSpan?> GetEmailCooldownAsync(string email, CancellationToken cancellationToken = default);

        /// <summary>Lưu bản chờ + sinh mã OTP 6 số. Trả registerToken (trả cho client) và mã OTP (gửi email).</summary>
        Task<(string RegisterToken, string Otp)> CreateAsync(PendingRegistration registration, CancellationToken cancellationToken = default);

        /// <summary>null nếu token không tồn tại/hết hạn/đã dùng/sai quá số lần.</summary>
        Task<PendingRegistration?> GetAsync(string registerToken, CancellationToken cancellationToken = default);

        /// <summary>Đúng mã → XÓA bản chờ (dùng 1 lần) và trả dữ liệu; sai → tăng đếm, sai quá giới hạn thì hủy bản chờ.</summary>
        Task<PendingRegistration?> ConsumeAsync(string registerToken, string otp, CancellationToken cancellationToken = default);

        /// <summary>Còn bao lâu mới được gửi lại mã cho bản chờ này; null = gửi được.</summary>
        Task<TimeSpan?> GetResendCooldownAsync(string registerToken, CancellationToken cancellationToken = default);

        /// <summary>Sinh mã mới + gia hạn bản chờ. null nếu token không còn hoặc đã gửi lại quá số lần.</summary>
        Task<string?> RegenerateOtpAsync(string registerToken, CancellationToken cancellationToken = default);
    }
}
