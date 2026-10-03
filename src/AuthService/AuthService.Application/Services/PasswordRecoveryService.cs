using AuthService.Application.Contracts;
using AuthService.Application.Interfaces;
using AuthService.Application.Options;
using Microsoft.Extensions.Options;

namespace AuthService.Application.Services
{
    /// <inheritdoc cref="IPasswordRecoveryService" />
    public class PasswordRecoveryService : IPasswordRecoveryService
    {
        private readonly IUserRepository _userRepository;
        private readonly ISessionRepository _sessionRepository;
        private readonly ISessionRevocationCache _revocationCache;
        private readonly IOtpService _otpService;
        private readonly ITotpSecretEncryption _totpEncryption;
        private readonly IOtpLockoutService _otpLockout;
        private readonly IPasswordResetService _passwordReset;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ICaptchaVerifier _captchaVerifier;
        private readonly IEmailSender _emailSender;
        private readonly ILoginHistoryRepository _loginHistory;
        private readonly AuthOptions _options;

        public PasswordRecoveryService(
            IUserRepository userRepository,
            ISessionRepository sessionRepository,
            ISessionRevocationCache revocationCache,
            IOtpService otpService,
            ITotpSecretEncryption totpEncryption,
            IOtpLockoutService otpLockout,
            IPasswordResetService passwordReset,
            IPasswordHasher passwordHasher,
            ICaptchaVerifier captchaVerifier,
            IEmailSender emailSender,
            ILoginHistoryRepository loginHistory,
            IOptions<AuthOptions> options)
        {
            _userRepository = userRepository;
            _sessionRepository = sessionRepository;
            _revocationCache = revocationCache;
            _otpService = otpService;
            _totpEncryption = totpEncryption;
            _otpLockout = otpLockout;
            _passwordReset = passwordReset;
            _passwordHasher = passwordHasher;
            _captchaVerifier = captchaVerifier;
            _emailSender = emailSender;
            _loginHistory = loginHistory;
            _options = options.Value;
        }

        public async Task ForgotPasswordAsync(ForgotPasswordRequest request, string remoteIp, CancellationToken cancellationToken = default)
        {
            if (!await _captchaVerifier.VerifyAsync(request.CaptchaToken, remoteIp, cancellationToken))
                throw new AuthDomainException(AuthErrorCode.CaptchaRequired, "Cần xác thực chống bot.");

            var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);

            // Mục 39 — không phân biệt username tồn tại hay không, kể cả khi đang cooldown: luôn
            // "thành công" từ góc nhìn caller, chỉ khác là có gửi email thật hay không.
            if (user == null)
                return;

            if (await _passwordReset.IsCooldownActiveAsync(user.Id, cancellationToken))
                return;

            var otp = await _passwordReset.GenerateAndStoreOtpAsync(user.Id, cancellationToken);
            await _passwordReset.SetCooldownAsync(user.Id, cancellationToken);

            await _emailSender.SendAsync(
                user.Email,
                "Mã xác nhận đặt lại mật khẩu",
                $"Mã xác nhận của bạn là: {otp}. Mã có hiệu lực trong {_options.PasswordResetOtpTtl.TotalMinutes:0} phút.",
                cancellationToken);
        }

        public async Task<VerifyResetOtpResponse> VerifyResetOtpAsync(VerifyResetOtpRequest request, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
            if (user == null || !await _passwordReset.VerifyOtpAsync(user.Id, request.Otp, cancellationToken))
                throw new AuthDomainException(AuthErrorCode.ResetOtpInvalid, "Mã xác nhận không hợp lệ hoặc đã hết hạn.");

            var resetToken = await _passwordReset.IssueResetTokenAsync(user.Id, cancellationToken);
            return new VerifyResetOtpResponse { ResetToken = resetToken };
        }

        public async Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request, string remoteIp, CancellationToken cancellationToken = default)
        {
            var userId = await _passwordReset.ConsumeResetTokenAsync(request.ResetToken, cancellationToken);
            if (userId == null)
                throw new AuthDomainException(AuthErrorCode.ResetTokenInvalid, "Yêu cầu đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");

            var user = await _userRepository.GetByIdAsync(userId!.Value, cancellationToken)
                ?? throw new AuthDomainException(AuthErrorCode.ResetTokenInvalid, "Tài khoản không tồn tại.");

            PasswordPolicy.Validate(request.NewPassword);

            await _userRepository.UpdatePasswordAsync(user.Id, _passwordHasher.Hash(request.NewPassword), "system_password_reset", cancellationToken);

            // Mục 36 — gỡ mọi khóa (kể cả vĩnh viễn) + reset toàn bộ counter.
            await _otpLockout.ClearAllLocksAsync(user.Id, cancellationToken);
            await _userRepository.SetPermanentlyLockedAsync(user.Id, false, "system_password_reset", cancellationToken);

            // Mục 42 — revoke hết session cũ vì đây là luồng nghi lộ thông tin.
            await SessionRevocationHelper.RevokeAllAndMarkCacheAsync(
                _sessionRepository, _revocationCache, user.Id, "system_password_reset", _options.SessionAbsoluteTtl, cancellationToken);

            // Mục 36 — sinh lại TOTP secret mới toàn bộ, y hệt luồng /auth/register.
            var newTotpSecret = _otpService.GenerateSecret();
            await _userRepository.UpdateTotpSecretAsync(user.Id, _totpEncryption.Encrypt(newTotpSecret), true, "system_password_reset", cancellationToken);

            await _loginHistory.InsertAsync(user.Id, user.Username, remoteIp, null, "password_reset", true, null, cancellationToken);

            return new ResetPasswordResponse { OtpauthUri = _otpService.BuildOtpauthUri(newTotpSecret, user.Username) };
        }
    }
}
