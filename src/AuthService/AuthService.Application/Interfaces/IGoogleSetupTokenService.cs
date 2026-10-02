namespace AuthService.Application.Interfaces
{
    /// <summary>Redis-backed — setupToken cho user mới đăng ký qua Google (mục 46/47). Implement ở Infrastructure.</summary>
    public interface IGoogleSetupTokenService
    {
        Task<string> IssueSetupTokenAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>Verify + XÓA setupToken (dùng 1 lần). Trả null nếu không hợp lệ/hết hạn/đã dùng.</summary>
        Task<Guid?> ConsumeSetupTokenAsync(string setupToken, CancellationToken cancellationToken = default);
    }
}
