namespace AuthService.Application.Contracts
{
    /// <summary>Cách đăng nhập — khớp cột login_history.login_type, dùng để chọn email cảnh báo phù hợp.</summary>
    public static class LoginMethods
    {
        public const string Otp = "otp";
        public const string PasswordFallback = "password_fallback";
        public const string Qr = "qr";
    }
}
