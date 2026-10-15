using AuthService.Application.Contracts;
using AuthService.Domain;

namespace AuthService.Application.Interfaces
{
    /// <summary>Implement ở Infrastructure, gọi sp_session_* qua PostgresDbHelper (PLAN.md mục 6).</summary>
    public interface ISessionRepository
    {
        Task<Session> CreateAsync(Session session, CancellationToken cancellationToken = default);
        Task<Session?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tìm session khớp hash ở CẢ refresh_token_hash lẫn previous_refresh_token_hash (mục 20).
        /// <c>IsCurrentMatch = false</c> nghĩa là khớp previous → refresh token đã bị rotate mà còn
        /// bị dùng lại → nghi bị đánh cắp, caller phải revoke toàn bộ session ngay.
        /// </summary>
        Task<(Session Session, bool IsCurrentMatch)?> FindByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken cancellationToken = default);

        /// <summary>Chỉ xoay khi refresh_token_hash hiện tại đúng bằng <paramref name="oldRefreshTokenHash"/> (compare-and-swap). false = đã bị request khác xoay/thu hồi.</summary>
        Task<bool> RotateRefreshTokenAsync(Guid id, string oldRefreshTokenHash, string newRefreshTokenHash, string updatedUser, CancellationToken cancellationToken = default);

        Task<List<Session>> GetActiveByUserAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>Admin xem TẤT CẢ session active toàn hệ thống — trả thẳng SessionDto (đã JOIN username ở DB).</summary>
        Task<List<SessionDto>> GetAllActiveAsync(CancellationToken cancellationToken = default);

        Task<bool> RevokeAsync(Guid id, string updatedUser, CancellationToken cancellationToken = default);

        /// <summary>Revoke mọi session còn sống, trả về ĐÚNG danh sách id vừa revoke (mục 33/34/42/disable/delete) — kể cả session tạo ngay trước đó.</summary>
        Task<List<Guid>> RevokeAllByUserAsync(Guid userId, string updatedUser, CancellationToken cancellationToken = default);
    }
}
