namespace AuthService.Domain
{
    /// <summary>
    /// PLAN.md mục 3/4b. PasswordHash/TotpSecret nullable — user mới qua Google (mục 46)
    /// chưa hoàn tất setup thì cả 2 đều null cho tới khi gọi /auth/register/google/complete.
    /// </summary>
    public class User : AuditableEntity
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PasswordHash { get; set; }
        public string? GoogleId { get; set; }
        public string? TotpSecret { get; set; }
        public bool IsTotpEnabled { get; set; }
        public RoleLevel RoleLevel { get; set; }
        public int FailedOtpAttemptCount { get; set; }
        public bool IsActive { get; set; }
        public bool IsPermanentlyLocked { get; set; }
    }
}
