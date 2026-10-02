using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.Options
{
    /// <summary>Bind từ section "Smtp" — gửi mã OTP reset password qua email (PLAN.md mục 38).</summary>
    public class SmtpOptions
    {
        public const string SectionName = "Smtp";

        [Required]
        public string Host { get; set; } = string.Empty;

        [Range(1, 65535)]
        public int Port { get; set; } = 587;

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string From { get; set; } = string.Empty;
    }
}
