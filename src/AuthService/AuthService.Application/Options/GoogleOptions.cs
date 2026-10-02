using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.Options
{
    /// <summary>Bind từ section "Google" — Google Sign-In / OIDC (PLAN.md mục 45).</summary>
    public class GoogleOptions
    {
        public const string SectionName = "Google";

        /// <summary>OAuth Client ID — dùng để check claim "aud" trong ID Token, không phải bí mật.</summary>
        [Required]
        public string ClientId { get; set; } = string.Empty;
    }
}
