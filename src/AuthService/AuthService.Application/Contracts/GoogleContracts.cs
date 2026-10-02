namespace AuthService.Application.Contracts
{
    /// <summary>PLAN.md mục 45/46 — POST /auth/login/google. Google KHÔNG BAO GIỜ cấp token trực tiếp.</summary>
    public class GoogleLoginRequest
    {
        public string IdToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// Đúng 1 trong 2 nhánh sẽ có giá trị (mục 46):
    /// - Email chưa tồn tại → RequiresSetup=true + SetupToken.
    /// - Email đã tồn tại (đã/chưa link) → RequiresOtp=true + Username, KHÔNG có token nào ở đây.
    /// </summary>
    public class GoogleLoginResponse
    {
        public bool RequiresSetup { get; set; }
        public string? SetupToken { get; set; }

        public bool RequiresOtp { get; set; }
        public string? Username { get; set; }
    }

    public class GoogleCompleteSetupRequest
    {
        public string SetupToken { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string CaptchaToken { get; set; } = string.Empty;
    }

    public class GoogleCompleteSetupResponse
    {
        public string OtpauthUri { get; set; } = string.Empty;
    }
}
