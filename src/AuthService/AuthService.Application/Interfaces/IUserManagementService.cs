using Shared.Common.Contracts;
using AuthService.Application.Contracts;
using AuthService.Domain;

namespace AuthService.Application.Interfaces
{
    /// <summary>Application Service — quản lý user, chỉ Admin (PLAN.md mục 5b/14).</summary>
    public interface IUserManagementService
    {
        Task<PagedResult<UserDto>> GetUsersAsync(RoleLevel? roleLevel, int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>GET /auth/me — cache-aside qua IUserProfileCache (mục 5b phát sinh), tự user xem chính mình.</summary>
        Task<UserDto> GetMyProfileAsync(Guid userId, CancellationToken cancellationToken = default);

        Task ChangeRoleAsync(Guid actorId, RoleLevel actorRole, string actorUsername, Guid targetId, RoleLevel newRoleLevel, CancellationToken cancellationToken = default);

        Task DisableAsync(Guid actorId, RoleLevel actorRole, string actorUsername, Guid targetId, CancellationToken cancellationToken = default);

        Task DeleteAsync(Guid actorId, RoleLevel actorRole, string actorUsername, Guid targetId, CancellationToken cancellationToken = default);

        /// <summary>Mở khóa OTP-lock/account-lock/khóa vĩnh viễn (mục 19/37) — không cần check self-target.</summary>
        Task UnlockAsync(string actorUsername, Guid targetId, CancellationToken cancellationToken = default);
    }
}
