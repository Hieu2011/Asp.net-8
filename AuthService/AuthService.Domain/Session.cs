namespace AuthService.Domain
{
    /// <summary>
    /// PLAN.md mục 3/4b. RefreshTokenHash/PreviousRefreshTokenHash — SHA256 (mục 31),
    /// rotation ghi đè cùng 1 dòng, không tạo dòng mới (mục 20).
    /// </summary>
    public class Session : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string? DeviceInfo { get; set; }
        public string? IpAddress { get; set; }
        public string RefreshTokenHash { get; set; } = string.Empty;
        public string? PreviousRefreshTokenHash { get; set; }
        public DateTimeOffset LastActivityAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public bool IsRevoked { get; set; }
    }
}
