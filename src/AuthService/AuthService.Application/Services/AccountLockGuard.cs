using AuthService.Application.Interfaces;
using AuthService.Domain;

namespace AuthService.Application.Services
{
    /// <summary>
    /// Check trạng thái tài khoản (mục 40) — dùng chung ở <see cref="AuthenticationService"/> (login OTP/password),
    /// <see cref="GoogleAuthService"/>, <see cref="QrLoginService"/> để không lặp lại logic.
    /// </summary>
    internal static class AccountLockGuard
    {
        /// <param name="isOtpLockChecked">false ở luồng password dự phòng — OTP bị khóa chính là điều kiện để được nhập password.</param>
        public static async Task EnsureNotLockedAsync(User user, IOtpLockoutService otpLockout, CancellationToken cancellationToken, bool isOtpLockChecked = true)
        {
            if (!user.IsActive)
                throw new AuthDomainException(AuthErrorCode.AccountDisabled, "Tài khoản đã bị vô hiệu hóa.");

            if (user.IsPermanentlyLocked)
                throw new AuthDomainException(AuthErrorCode.AccountPermanentlyLocked, "Tài khoản đã bị khóa.");

            var accountLockRemaining = await otpLockout.GetAccountLockRemainingAsync(user.Id, cancellationToken);
            if (accountLockRemaining.HasValue)
                throw new AuthDomainException(AuthErrorCode.AccountLocked, "Tài khoản đang tạm khóa.", accountLockRemaining);

            if (!isOtpLockChecked)
                return;

            var otpLockRemaining = await otpLockout.GetOtpLockRemainingAsync(user.Id, cancellationToken);
            if (otpLockRemaining.HasValue)
                throw new AuthDomainException(AuthErrorCode.OtpLocked, "Đăng nhập OTP đang tạm khóa.", otpLockRemaining);
        }
    }
}
