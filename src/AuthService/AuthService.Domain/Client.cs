namespace AuthService.Domain
{
    /// <summary>
    /// Đối tác ngoài, Flow B — OAuth2 Client Credentials Grant (PLAN.md mục 3/4b/22).
    /// </summary>
    public class Client : AuditableEntity
    {
        public Guid Id { get; set; }
        public string ClientId { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string ClientSecretHash { get; set; } = string.Empty;
        public List<string> AllowedScopes { get; set; } = new();
        public List<string>? AllowedIps { get; set; }
        public bool IsActive { get; set; }
    }
}
