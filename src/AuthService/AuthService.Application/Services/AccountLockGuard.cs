using AuthService.Application.Interfaces;
using AuthService.Domain;

namespace AuthService.Application.Services
{
    /// <summary>
    /// Check trạng thái khóa (mục 40) — dùng chung ở <see cref="AuthenticationService"/> (login OTP/password)
    /// và <see cref="GoogleAuthService"/> (login qua Google) để không lặp lại logic.
    /// </summary>
    internal static class AccountLockGuard
    {
        public static async Task EnsureNotLockedAsync(User user, IOtpLockoutService otpLockout, CancellationToken cancellationToken)
        {
            if (user.IsPermanentlyLocked)
                throw new AuthDomainException(AuthErrorCode.AccountPermanentlyLocked, "Tài khoản đã bị khóa.");

            var accountLockRemaining = await otpLockout.GetAccountLockRemainingAsync(user.Id, cancellationToken);
            if (accountLockRemaining.HasValue)
                throw new AuthDomainException(AuthErrorCode.AccountLocked, "Tài khoản đang tạm khóa.", accountLockRemaining);

            var otpLockRemaining = await otpLockout.GetOtpLockRemainingAsync(user.Id, cancellationToken);
            if (otpLockRemaining.HasValue)
                throw new AuthDomainException(AuthErrorCode.OtpLocked, "Đăng nhập OTP đang tạm khóa.", otpLockRemaining);
        }
    }
}
