using System.Text.RegularExpressions;

namespace AuthService.Application
{
    /// <summary>Họ tên khi đăng ký: 2–100 ký tự, chữ (có dấu tiếng Việt), khoảng trắng, dấu nháy/chấm/gạch.</summary>
    public static partial class FullNamePolicy
    {
        public const int MaxLength = 100;
        private const int MinLength = 2;

        [GeneratedRegex(@"^[\p{L}\p{M}][\p{L}\p{M}' .\-]*$")]
        private static partial Regex Pattern();

        [GeneratedRegex(@"\s+")]
        private static partial Regex Whitespace();

        /// <summary>Trả họ tên đã gộp khoảng trắng thừa để lưu DB.</summary>
        public static string Validate(string fullName)
        {
            var strFullName = Normalize(fullName);
            if (strFullName.Length < MinLength || strFullName.Length > MaxLength || !Pattern().IsMatch(strFullName))
                throw new AuthDomainException(AuthErrorCode.FullNameInvalid, "Họ tên 2-100 ký tự, chỉ gồm chữ và khoảng trắng.");
            return strFullName;
        }

        /// <summary>Tên lấy từ Google: chỉ gộp khoảng trắng + cắt độ dài, không chặn ký tự (tên do Google cấp).</summary>
        public static string? FromExternal(string? fullName)
        {
            var strFullName = Normalize(fullName);
            if (strFullName.Length == 0) return null;
            return strFullName.Length > MaxLength ? strFullName[..MaxLength].TrimEnd() : strFullName;
        }

        private static string Normalize(string? fullName) => Whitespace().Replace(fullName ?? string.Empty, " ").Trim();
    }
}
