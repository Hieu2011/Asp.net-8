using Shared.Common.Contracts;
namespace AuthService.Application
{
    /// <summary>
    /// Dùng làm <c>APIResult.StatusID</c> — client phân biệt các case lỗi cụ thể của Auth
    /// (PLAN.md mục 40). Mở rộng thêm giá trị khi code tới controller/service cần case mới.
    /// </summary>
    public enum AuthErrorCode
    {
        None = 0,
        InvalidCredential = 1,
        OtpLocked = 2,
        AccountLocked = 3,
        AccountPermanentlyLocked = 4,
        OtpFallbackNotAllowed = 5,
        CaptchaRequired = 6,
        CaptchaInvalid = 7,
        PasswordPolicyViolation = 8,
        ResetOtpInvalid = 9,
        ResetOtpExpired = 10,
        ResetTokenInvalid = 11,
        RefreshTokenInvalid = 12,
        RefreshTokenReuseDetected = 13,
        SessionNotFound = 14,
        Forbidden = 15,
        SelfTargetNotAllowed = 16,
        GoogleTokenInvalid = 17,
        GoogleEmailNotVerified = 18,
        GoogleSetupTokenInvalid = 19,
        ClientCredentialInvalid = 20,
        ClientIpNotAllowed = 21,
        ClientDisabled = 22,
        UsernameAlreadyExists = 23,
        EmailAlreadyExists = 24,
        ResetOtpCooldownActive = 25,
        UserNotFound = 26
    }
}
