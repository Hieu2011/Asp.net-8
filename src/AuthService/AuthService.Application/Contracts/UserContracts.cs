using AuthService.Domain;

namespace AuthService.Application.Contracts
{
    /// <summary>PLAN.md mục 5 — dùng cho GET /auth/users. Không lộ PasswordHash/TotpSecret ra ngoài.</summary>
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public RoleLevel RoleLevel { get; set; }
        public bool IsActive { get; set; }
        public bool IsPermanentlyLocked { get; set; }
        public bool IsTotpEnabled { get; set; }
        public bool HasGoogleLinked { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    /// <summary>PUT /auth/users/{id}/role — mục 34, tự revoke session user đó khi đổi role.</summary>
    public class ChangeUserRoleRequest
    {
        public RoleLevel NewRoleLevel { get; set; }
    }
}
