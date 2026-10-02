using System.Text.RegularExpressions;

namespace AuthService.Application
{
    /// <summary>8–12 ký tự, ≥1 chữ hoa, ≥1 ký tự đặc biệt (PLAN.md mục 27). Dùng chung ở register/reset/Google complete.</summary>
    public static partial class PasswordPolicy
    {
        [GeneratedRegex(@"^(?=.*[A-Z])(?=.*[^a-zA-Z0-9]).{8,12}$")]
        private static partial Regex Pattern();

        public static void Validate(string password)
        {
            if (string.IsNullOrEmpty(password) || !Pattern().IsMatch(password))
            {
                throw new AuthDomainException(AuthErrorCode.PasswordPolicyViolation,
                    "Mật khẩu phải 8-12 ký tự, có ít nhất 1 chữ hoa và 1 ký tự đặc biệt.");
            }
        }
    }
}
