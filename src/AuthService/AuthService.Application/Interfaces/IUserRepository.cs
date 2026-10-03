using Shared.Common.Contracts;
using AuthService.Application.Contracts;
using AuthService.Domain;

namespace AuthService.Application.Interfaces
{
    /// <summary>Implement ở Infrastructure, gọi sp_user_* qua PostgresDbHelper (PLAN.md mục 6).</summary>
    public interface IUserRepository
    {
        Task<User> CreateAsync(User user, CancellationToken cancellationToken = default);
        Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<User?> GetByGoogleIdAsync(string googleId, CancellationToken cancellationToken = default);
        Task<PagedResult<User>> GetPagedAsync(RoleLevel? roleLevel, int page, int pageSize, CancellationToken cancellationToken = default);

        Task<bool> UpdatePasswordAsync(Guid id, string passwordHash, string updatedUser, CancellationToken cancellationToken = default);

        /// <summary>totpSecret truyền vào đã là chuỗi MÃ HÓA AES (mục 13) — repository không tự mã hóa.</summary>
        Task<bool> UpdateTotpSecretAsync(Guid id, string? totpSecret, bool isTotpEnabled, string updatedUser, CancellationToken cancellationToken = default);

        Task<bool> LinkGoogleAsync(Guid id, string googleId, string updatedUser, CancellationToken cancellationToken = default);
        Task<bool> UpdateRoleAsync(Guid id, RoleLevel roleLevel, string updatedUser, CancellationToken cancellationToken = default);
        Task<bool> SetActiveAsync(Guid id, bool isActive, string updatedUser, CancellationToken cancellationToken = default);

        /// <summary>true = khóa vĩnh viễn (mục 35 escalation); false = gỡ khóa (mục 36/37).</summary>
        Task<bool> SetPermanentlyLockedAsync(Guid id, bool isPermanentlyLocked, string updatedUser, CancellationToken cancellationToken = default);

        /// <summary>Đồng bộ cột audit theo counter Redis — không phải nguồn đúng cho logic khóa (mục 17-18).</summary>
        Task<bool> SetFailedOtpCountAsync(Guid id, int count, CancellationToken cancellationToken = default);

        Task<bool> SoftDeleteAsync(Guid id, string deletedUser, CancellationToken cancellationToken = default);
    }
}
