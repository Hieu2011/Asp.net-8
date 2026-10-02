using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.Options
{
    /// <summary>Bind từ section "Turnstile" — Cloudflare Turnstile anti-bot (PLAN.md mục 44).</summary>
    public class TurnstileOptions
    {
        public const string SectionName = "Turnstile";

        /// <summary>Secret dùng verify server-side — bí mật, User Secrets.</summary>
        [Required]
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>Site key public — frontend dùng để render widget, không phải bí mật.</summary>
        [Required]
        public string SiteKey { get; set; } = string.Empty;
    }
}
