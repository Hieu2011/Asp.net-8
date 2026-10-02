using AuthService.Application.Interfaces;
using AuthService.Domain;

namespace AuthService.Infrastructure.Security
{
    /// <inheritdoc cref="IRoleAuthorizationService" />
    public class RoleAuthorizationService : IRoleAuthorizationService
    {
        // Số càng nhỏ, quyền càng cao (mục 2) — actor đủ quyền khi level của actor <= ngưỡng yêu cầu.
        public bool CanAccess(RoleLevel actorRole, RoleLevel requiredMin) => (int)actorRole <= (int)requiredMin;

        public bool IsAdmin(RoleLevel actorRole) => actorRole == RoleLevel.Admin;

        public bool IsSelfTarget(Guid actorId, Guid targetId) => actorId == targetId;
    }
}
