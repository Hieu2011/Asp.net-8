namespace AuthService.Application.Contracts
{
    /// <summary>PLAN.md mục 5 — dùng cho GET /auth/sessions, /auth/admin/sessions, /auth/users/{id}/sessions.</summary>
    public class SessionDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string? Username { get; set; }
        public string? DeviceInfo { get; set; }
        public string? IpAddress { get; set; }
        public DateTime LastActivityAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
