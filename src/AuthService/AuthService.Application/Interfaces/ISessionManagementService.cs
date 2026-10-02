using AuthService.Application.Contracts;
using AuthService.Domain;

namespace AuthService.Application.Interfaces
{
    /// <summary>Application Service — quản lý session, cả tự phục vụ lẫn Admin (PLAN.md mục 5b/7).</summary>
    public interface ISessionManagementService
    {
        Task<List<SessionDto>> GetMySessionsAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>Tự thu hồi 1 session — tự verify session đó THUỘC VỀ đúng user gọi (chặn IDOR qua đoán ID).</summary>
        Task RevokeSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default);

        Task RevokeAllMySessionsAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>Admin/Manager xem TẤT CẢ session active toàn hệ thống.</summary>
        Task<List<SessionDto>> GetAllActiveSessionsAsync(CancellationToken cancellationToken = default);

        Task<List<SessionDto>> GetUserSessionsAsync(Guid targetUserId, CancellationToken cancellationToken = default);

        Task RevokeUserSessionsAsync(RoleLevel actorRole, string actorUsername, Guid targetUserId, CancellationToken cancellationToken = default);

        Task RevokeUserSessionAsync(RoleLevel actorRole, string actorUsername, Guid targetUserId, Guid sessionId, CancellationToken cancellationToken = default);
    }
}
