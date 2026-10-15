namespace AuthService.Application.Interfaces
{
    /// <summary>Redis-backed — luồng quên mật khẩu/cấp lại quyền qua email OTP (mục 36/41). Implement ở Infrastructure.</summary>
    public interface IPasswordResetService
    {
        /// <summary>Xin quyền gửi mã mới (nguyên tử): false nếu đang cooldown 60s, đã gửi quá số mã/ngày
        /// hoặc nhập sai quá số lần/ngày. true thì đã tự đặt cooldown.</summary>
        Task<bool> TryReserveSendAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>Sinh mã 6 số, lưu hash vào Redis, trả về mã PLAINTEXT để Service gửi qua email.</summary>
        Task<string> GenerateAndStoreOtpAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>Verify OTP nguyên tử — sai 5 lần/mã thì xóa mã (mục 41), sai quá số lần/ngày thì khóa luồng reset tới hết ngày.</summary>
        Task<bool> VerifyOtpAsync(Guid userId, string otp, CancellationToken cancellationToken = default);

        /// <summary>Sinh resetToken (random), lưu hash → userId, TTL riêng (mục 6). Trả token PLAINTEXT cho client.</summary>
        Task<string> IssueResetTokenAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>Verify + XÓA resetToken (dùng 1 lần). Trả null nếu không hợp lệ/hết hạn/đã dùng.</summary>
        Task<Guid?> ConsumeResetTokenAsync(string resetToken, CancellationToken cancellationToken = default);
    }
}
