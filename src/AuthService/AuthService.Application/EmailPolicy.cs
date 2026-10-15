using System.Text.RegularExpressions;

namespace AuthService.Application
{
    /// <summary>Email là định danh đăng nhập — luôn so sánh/lưu dạng chữ thường, bỏ khoảng trắng.</summary>
    public static partial class EmailPolicy
    {
        private const int MaxLength = 254;

        // Đủ chặt cho form đăng ký: 1 @, có tên miền + đuôi, không khoảng trắng
        [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$")]
        private static partial Regex Pattern();

        /// <summary>Kiểm tra định dạng, trả email đã chuẩn hóa để lưu DB.</summary>
        public static string Validate(string email)
        {
            var strEmail = Normalize(email);
            if (strEmail.Length == 0 || strEmail.Length > MaxLength || !Pattern().IsMatch(strEmail))
                throw new AuthDomainException(AuthErrorCode.EmailInvalid, "Email không hợp lệ.");
            return strEmail;
        }

        /// <summary>Chuẩn hóa email người dùng nhập khi đăng nhập/quên mật khẩu (không báo lỗi định dạng để tránh dò tài khoản).</summary>
        public static string Normalize(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();
    }
}
