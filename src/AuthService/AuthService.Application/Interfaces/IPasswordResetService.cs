namespace AuthService.Application.Interfaces
{
    /// <summary>Redis-backed — luồng quên mật khẩu/cấp lại quyền qua email OTP (mục 36/41). Implement ở Infrastructure.</summary>
    public interface IPasswordResetService
    {
        Task<bool> IsCooldownActiveAsync(Guid userId, CancellationToken cancellationToken = default);
        Task SetCooldownAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>Sinh mã 6 số, lưu Redis, trả về mã PLAINTEXT để Service gửi qua email.</summary>
        Task<string> GenerateAndStoreOtpAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>Verify OTP — tự đếm sai (mục 41), vượt 5 lần thì tự xóa OTP luôn (buộc xin mã mới).</summary>
        Task<bool> VerifyOtpAsync(Guid userId, string otp, CancellationToken cancellationToken = default);

        /// <summary>Sinh resetToken (random), lưu hash → userId, TTL riêng (mục 6). Trả token PLAINTEXT cho client.</summary>
        Task<string> IssueResetTokenAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>Verify + XÓA resetToken (dùng 1 lần). Trả null nếu không hợp lệ/hết hạn/đã dùng.</summary>
        Task<Guid?> ConsumeResetTokenAsync(string resetToken, CancellationToken cancellationToken = default);
    }
}
