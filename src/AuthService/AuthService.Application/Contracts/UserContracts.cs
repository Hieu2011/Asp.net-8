using AuthService.Domain;

namespace AuthService.Application.Contracts
{
    /// <summary>PLAN.md mục 5 — dùng cho GET /auth/users. Không lộ PasswordHash/TotpSecret ra ngoài.</summary>
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public RoleLevel RoleLevel { get; set; }
        public bool IsActive { get; set; }
        public bool IsPermanentlyLocked { get; set; }
        public bool IsTotpEnabled { get; set; }
        public bool HasGoogleLinked { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    /// <summary>GET /auth/admin/stats — số liệu thẻ Tổng quan trang quản trị (FE-11).</summary>
    public class UserStatsDto
    {
        public long TotalUsers { get; set; }
        public long ActiveUsers { get; set; }
        public long DisabledUsers { get; set; }
        public long LockedUsers { get; set; }
        public long GoogleLinkedUsers { get; set; }
        public long NewUsers7Days { get; set; }
    }

    /// <summary>PUT /auth/users/{id}/role — mục 34, tự revoke session user đó khi đổi role.</summary>
    public class ChangeUserRoleRequest
    {
        public RoleLevel NewRoleLevel { get; set; }
    }
}
