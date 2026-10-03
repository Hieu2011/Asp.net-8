using AuthService.Domain;

namespace AuthService.Application.Interfaces
{
    /// <summary>RBAC — PLAN.md mục 2/14.</summary>
    public interface IRoleAuthorizationService
    {
        /// <summary>actor.Level <= requiredMin.Level → cho phép. Dùng cho ngưỡng cố định.</summary>
        bool CanAccess(RoleLevel actorRole, RoleLevel requiredMin);

        /// <summary>Chỉ Admin (cấp 1) mới được đổi role/disable/xóa/thu hồi session của user khác.</summary>
        bool IsAdmin(RoleLevel actorRole);

        /// <summary>Chặn actor thao tác lên chính mình (mục 2/14).</summary>
        bool IsSelfTarget(Guid actorId, Guid targetId);
    }
}
