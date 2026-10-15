using AuthService.Application.Contracts;
using AuthService.Application.Interfaces;
using AuthService.Application.Options;
using AuthService.Domain;
using Microsoft.Extensions.Options;

namespace AuthService.Application.Services
{
    /// <inheritdoc cref="IAuthService" />
    public class AuthenticationService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ISessionRepository _sessionRepository;
        private readonly IOtpService _otpService;
        private readonly IJwtService _jwtService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITotpSecretEncryption _totpEncryption;
        private readonly IOtpLockoutService _otpLockout;
        private readonly ICaptchaVerifier _captchaVerifier;
        private readonly ILoginHistoryRepository _loginHistory;
        private readonly ISessionRevocationCache _revocationCache;
        private readonly IPendingRegistrationService _pendingRegistration;
        private readonly IEmailSender _emailSender;
        private readonly ISessionTokenIssuer _tokenIssuer;
        private readonly ILoginEmailRateLimiter _emailRateLimiter;
        private readonly AuthOptions _options;

        public AuthenticationService(
            IUserRepository userRepository,
            ISessionRepository sessionRepository,
            IOtpService otpService,
            IJwtService jwtService,
            IPasswordHasher passwordHasher,
            ITotpSecretEncryption totpEncryption,
            IOtpLockoutService otpLockout,
            ICaptchaVerifier captchaVerifier,
            ILoginHistoryRepository loginHistory,
            ISessionRevocationCache revocationCache,
            IPendingRegistrationService pendingRegistration,
            IEmailSender emailSender,
            ISessionTokenIssuer tokenIssuer,
            ILoginEmailRateLimiter emailRateLimiter,
            IOptions<AuthOptions> options)
        {
            _userRepository = userRepository;
            _sessionRepository = sessionRepository;
            _otpService = otpService;
            _jwtService = jwtService;
            _passwordHasher = passwordHasher;
            _totpEncryption = totpEncryption;
            _otpLockout = otpLockout;
            _captchaVerifier = captchaVerifier;
            _loginHistory = loginHistory;
            _revocationCache = revocationCache;
            _pendingRegistration = pendingRegistration;
            _emailSender = emailSender;
            _tokenIssuer = tokenIssuer;
            _emailRateLimiter = emailRateLimiter;
            _options = options.Value;
        }

        public async Task<RegisterOtpSentResponse> RegisterAsync(RegisterRequest request, string remoteIp, CancellationToken cancellationToken = default)
        {
            if (!await _captchaVerifier.VerifyAsync(request.CaptchaToken, remoteIp, cancellationToken))
                throw new AuthDomainException(AuthErrorCode.CaptchaInvalid, "Xác thực chống bot không hợp lệ.");

            var strFullName = FullNamePolicy.Validate(request.FullName);
            var strEmail = EmailPolicy.Validate(request.Email);
            PasswordPolicy.Validate(request.Password);

            if (await _userRepository.GetByEmailAsync(strEmail, cancellationToken) != null)
                throw new AuthDomainException(AuthErrorCode.EmailAlreadyExists, "Email đã được sử dụng.");

            // Chống dùng form đăng ký để spam hộp thư người khác
            var tspCooldown = await _pendingRegistration.GetEmailCooldownAsync(strEmail, cancellationToken);
            if (tspCooldown.HasValue)
                throw new AuthDomainException(AuthErrorCode.RegisterOtpCooldown, "Vừa gửi mã tới email này, vui lòng chờ rồi thử lại.", tspCooldown);

            var objPending = new PendingRegistration { FullName = strFullName, Email = strEmail, PasswordHash = _passwordHasher.Hash(request.Password) };
            var (strRegisterToken, strOtp) = await _pendingRegistration.CreateAsync(objPending, cancellationToken);
            await SendRegisterOtpAsync(strEmail, strOtp, cancellationToken);
            return BuildOtpSentResponse(strRegisterToken, strEmail);
        }

        public async Task<RegisterResponse> VerifyRegisterAsync(RegisterVerifyRequest request, CancellationToken cancellationToken = default)
        {
            if (await _pendingRegistration.GetAsync(request.RegisterToken, cancellationToken) == null)
                throw new AuthDomainException(AuthErrorCode.RegisterTokenInvalid, "Mã xác nhận đã hết hạn, vui lòng đăng ký lại.");

            var objPending = await _pendingRegistration.ConsumeAsync(request.RegisterToken, request.Otp, cancellationToken)
                ?? throw new AuthDomainException(AuthErrorCode.RegisterOtpInvalid, "Mã xác nhận không đúng.");

            // Kiểm tra lại: trong lúc chờ nhập mã, email có thể đã được đăng ký bằng Google
            if (await _userRepository.GetByEmailAsync(objPending.Email, cancellationToken) != null)
                throw new AuthDomainException(AuthErrorCode.EmailAlreadyExists, "Email đã được sử dụng.");

            var strTotpSecret = _otpService.GenerateSecret();
            var objUser = new User
            {
                Email = objPending.Email,
                FullName = objPending.FullName,
                PasswordHash = objPending.PasswordHash,
                TotpSecret = _totpEncryption.Encrypt(strTotpSecret),
                RoleLevel = RoleLevel.Customer, // mục 7 — /auth/register công khai luôn ra cấp 5
                IsActive = true
            };

            var objCreated = await _userRepository.CreateAsync(objUser, cancellationToken);
            return new RegisterResponse { OtpauthUri = _otpService.BuildOtpauthUri(strTotpSecret, objCreated.Email) };
        }

        public async Task<RegisterOtpSentResponse> ResendRegisterOtpAsync(RegisterResendRequest request, CancellationToken cancellationToken = default)
        {
            var objPending = await _pendingRegistration.GetAsync(request.RegisterToken, cancellationToken)
                ?? throw new AuthDomainException(AuthErrorCode.RegisterTokenInvalid, "Phiên đăng ký đã hết hạn, vui lòng đăng ký lại.");

            var tspCooldown = await _pendingRegistration.GetResendCooldownAsync(request.RegisterToken, cancellationToken);
            if (tspCooldown.HasValue)
                throw new AuthDomainException(AuthErrorCode.RegisterOtpCooldown, "Vui lòng chờ rồi gửi lại mã.", tspCooldown);

            var strOtp = await _pendingRegistration.RegenerateOtpAsync(request.RegisterToken, cancellationToken)
                ?? throw new AuthDomainException(AuthErrorCode.RegisterTokenInvalid, "Đã gửi lại mã quá số lần cho phép, vui lòng đăng ký lại.");

            await SendRegisterOtpAsync(objPending.Email, strOtp, cancellationToken);
            return BuildOtpSentResponse(request.RegisterToken, objPending.Email);
        }

        public async Task<TokenResponse> LoginAsync(LoginRequest request, string remoteIp, string? deviceInfo, CancellationToken cancellationToken = default)
        {
            var strEmail = EmailPolicy.Normalize(request.Email);
            await EnsureEmailRateAsync(strEmail, cancellationToken);
            var user = await _userRepository.GetByEmailAsync(strEmail, cancellationToken);

            // Mục 40 — check khóa TRƯỚC verify OTP, nhưng CHỈ SAU KHI xác nhận username tồn tại.
            if (user != null)
                await AccountLockGuard.EnsureNotLockedAsync(user, _otpLockout, cancellationToken);

            if (user == null || string.IsNullOrEmpty(user.TotpSecret))
            {
                await _loginHistory.InsertAsync(user?.Id, strEmail, remoteIp, deviceInfo, "otp", false, "invalid_credential", cancellationToken);
                throw new AuthDomainException(AuthErrorCode.InvalidCredential, "Sai thông tin đăng nhập.");
            }

            // Giữ chỗ lượt thử trước khi verify — bắn song song nhiều request cũng chỉ tối đa 3 lượt được verify
            var intAttempt = await _otpLockout.TryReserveOtpAttemptAsync(user.Id, cancellationToken)
                ?? throw new AuthDomainException(AuthErrorCode.OtpLocked, "Đăng nhập OTP đang tạm khóa.",
                    await _otpLockout.GetOtpLockRemainingAsync(user.Id, cancellationToken));

            // Chỉ giữ lượt khi đã chắc là SAI MÃ — thiếu captcha, replay, lỗi mạng/Turnstile, client hủy request... đều trả lượt
            // (không trả thì kẻ tấn công ngắt kết nối giữa chừng để đẩy bộ đếm lên mà không cần đoán mã)
            var bolReleaseAttempt = true;
            try
            {
                // Mục 44 — bắt buộc captcha từ lượt thứ 2 (adaptive, không làm phiền lần đầu)
                if (intAttempt > 1 && !await _captchaVerifier.VerifyAsync(request.CaptchaToken ?? string.Empty, remoteIp, cancellationToken))
                    throw new AuthDomainException(AuthErrorCode.CaptchaRequired, "Cần xác thực chống bot.");

                var decryptedSecret = _totpEncryption.Decrypt(user.TotpSecret);
                if (!_otpService.TryVerify(decryptedSecret, request.Otp, out var matchedTimeStep))
                {
                    bolReleaseAttempt = false;
                    await _otpLockout.RecordOtpFailureAsync(user.Id, intAttempt, CancellationToken.None);
                    await _loginHistory.InsertAsync(user.Id, strEmail, remoteIp, deviceInfo, "otp", false, "wrong_otp", cancellationToken);
                    throw new AuthDomainException(AuthErrorCode.InvalidCredential, "Sai thông tin đăng nhập.");
                }

                // Mục 30 — chống replay cùng chu kỳ 30s
                if (!await _otpLockout.TryRecordOtpStepAsync(user.Id, matchedTimeStep, cancellationToken))
                {
                    await _loginHistory.InsertAsync(user.Id, strEmail, remoteIp, deviceInfo, "otp", false, "otp_replay", cancellationToken);
                    throw new AuthDomainException(AuthErrorCode.InvalidCredential, "Sai thông tin đăng nhập.");
                }
            }
            finally
            {
                if (bolReleaseAttempt)
                    await _otpLockout.ReleaseOtpAttemptAsync(user.Id, CancellationToken.None);
            }

            await _otpLockout.ResetOtpFailAsync(user.Id, cancellationToken);
            await _loginHistory.InsertAsync(user.Id, strEmail, remoteIp, deviceInfo, "otp", true, null, cancellationToken);

            return await _tokenIssuer.IssueAsync(user, remoteIp, deviceInfo, LoginMethods.Otp, cancellationToken);
        }

        public async Task<TokenResponse> LoginWithPasswordAsync(LoginWithPasswordRequest request, string remoteIp, string? deviceInfo, CancellationToken cancellationToken = default)
        {
            // Mục 44 — luôn bắt buộc captcha ở fallback (khác /auth/login, vì đây đã là trạng thái nghi ngờ)
            if (!await _captchaVerifier.VerifyAsync(request.CaptchaToken, remoteIp, cancellationToken))
                throw new AuthDomainException(AuthErrorCode.CaptchaRequired, "Cần xác thực chống bot.");

            var strEmail = EmailPolicy.Normalize(request.Email);
            await EnsureEmailRateAsync(strEmail, cancellationToken);
            var user = await _userRepository.GetByEmailAsync(strEmail, cancellationToken);

            if (user != null)
            {
                await AccountLockGuard.EnsureNotLockedAsync(user, _otpLockout, cancellationToken, isOtpLockChecked: false);

                // Mục 29 — gate: chỉ cho vào khi đường OTP ĐANG BỊ KHÓA (đã sai 3 lần thật). Không xét bộ đếm,
                // vì bộ đếm có thể tạm chạm 3 do lượt đang xử lý dở. Không có gate này thì OTP-first bị bypass hoàn toàn.
                if (!await _otpLockout.IsOtpLockedAsync(user.Id, cancellationToken))
                    throw new AuthDomainException(AuthErrorCode.OtpFallbackNotAllowed, "Vui lòng đăng nhập bằng OTP.");
            }

            if (user == null || string.IsNullOrEmpty(user.PasswordHash) || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                if (user != null)
                {
                    var (_, escalationCount) = await _otpLockout.IncrementPasswordFailAsync(user.Id, cancellationToken);
                    // Mục 35 — từ lần khóa tạm thứ 2 trở đi thì khóa vĩnh viễn thay vì khóa tạm lần nữa.
                    if (escalationCount >= 2)
                        await _userRepository.SetPermanentlyLockedAsync(user.Id, true, "system", cancellationToken);
                }

                await _loginHistory.InsertAsync(user?.Id, strEmail, remoteIp, deviceInfo, "password_fallback", false, "wrong_password", cancellationToken);
                throw new AuthDomainException(AuthErrorCode.InvalidCredential, "Sai thông tin đăng nhập.");
            }

            await _otpLockout.ResetPasswordFailAsync(user.Id, cancellationToken);
            await _otpLockout.ResetOtpFailAsync(user.Id, cancellationToken);
            await _loginHistory.InsertAsync(user.Id, strEmail, remoteIp, deviceInfo, "password_fallback", true, null, cancellationToken);

            return await _tokenIssuer.IssueAsync(user, remoteIp, deviceInfo, LoginMethods.PasswordFallback, cancellationToken);
        }

        public async Task<TokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            var refreshTokenHash = SessionTokenIssuer.HashRefreshToken(refreshToken);
            var match = await _sessionRepository.FindByRefreshTokenHashAsync(refreshTokenHash, cancellationToken);
            if (match == null)
                throw new AuthDomainException(AuthErrorCode.RefreshTokenInvalid, "Refresh token không hợp lệ.");

            var (session, isCurrentMatch) = match.Value;

            // Phiên đã chết thì token cũ chỉ đơn giản là hết hiệu lực — không để ai cầm token cũ đi đăng xuất user khỏi mọi thiết bị
            if (session.IsRevoked || session.ExpiresAt <= DateTime.UtcNow)
                throw new AuthDomainException(AuthErrorCode.RefreshTokenInvalid, "Phiên đăng nhập đã hết hạn hoặc bị thu hồi.");

            if (!isCurrentMatch)
            {
                // Vừa xoay trong cửa sổ ân hạn = 2 tab/2 request refresh cùng lúc, không phải bị đánh cắp
                if (DateTime.UtcNow - session.LastActivityAt <= _options.RefreshReuseGraceWindow)
                    throw new AuthDomainException(AuthErrorCode.RefreshTokenConcurrent, "Phiên vừa được làm mới ở request khác.");

                // Mục 20 — refresh token cũ (đã bị rotate) mà còn bị dùng lại: nghi bị đánh cắp,
                // revoke toàn bộ session của user này ngay.
                await SessionRevocationHelper.RevokeAllAndMarkCacheAsync(
                    _sessionRepository, _revocationCache, session.UserId, "system_reuse_detected", _options.SessionAbsoluteTtl, cancellationToken);
                throw new AuthDomainException(AuthErrorCode.RefreshTokenReuseDetected,
                    "Phát hiện refresh token bị dùng lại — toàn bộ phiên đăng nhập đã bị thu hồi.");
            }

            var user = await _userRepository.GetByIdAsync(session.UserId, cancellationToken);
            if (user == null || !user.IsActive || user.IsPermanentlyLocked)
                throw new AuthDomainException(AuthErrorCode.RefreshTokenInvalid, "Tài khoản không còn hiệu lực.");

            var newRefreshToken = SessionTokenIssuer.GenerateRefreshToken();
            // Compare-and-swap theo hash cũ — 2 request song song cùng refresh token thì chỉ 1 request được xoay
            if (!await _sessionRepository.RotateRefreshTokenAsync(session.Id, refreshTokenHash, SessionTokenIssuer.HashRefreshToken(newRefreshToken), user.Username, cancellationToken))
                throw new AuthDomainException(AuthErrorCode.RefreshTokenConcurrent, "Phiên vừa được làm mới ở request khác.");

            var accessToken = _jwtService.IssueAccessToken(user.Id, user.Username, user.RoleLevel, session.Id, _options.AccessTokenTtl);
            return new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                ExpiresIn = (int)_options.AccessTokenTtl.TotalSeconds
            };
        }

        public async Task LogoutAsync(Guid sessionId, string actorUsername, CancellationToken cancellationToken = default)
        {
            await _sessionRepository.RevokeAsync(sessionId, actorUsername, cancellationToken);
            await _revocationCache.MarkRevokedAsync(sessionId, _options.SessionAbsoluteTtl, cancellationToken);
        }

        // Đếm cả email không tồn tại — không để lộ email nào có thật qua việc có/không bị giới hạn
        private async Task EnsureEmailRateAsync(string strEmail, CancellationToken cancellationToken)
        {
            var tspRetryAfter = await _emailRateLimiter.TryAcquireAsync(strEmail, cancellationToken);
            if (tspRetryAfter.HasValue)
                throw new AuthDomainException(AuthErrorCode.TooManyRequests, "Thử đăng nhập quá nhiều lần với email này.", tspRetryAfter);
        }

        private Task SendRegisterOtpAsync(string strEmail, string strOtp, CancellationToken cancellationToken)
            => _emailSender.SendAsync(
                strEmail,
                "Mã xác nhận đăng ký tài khoản",
                $"Mã xác nhận đăng ký của bạn là: {strOtp}. Mã có hiệu lực trong {_options.RegisterOtpTtl.TotalMinutes:0} phút. "
                + "Nếu bạn không đăng ký tài khoản, hãy bỏ qua email này.",
                cancellationToken);

        private RegisterOtpSentResponse BuildOtpSentResponse(string strRegisterToken, string strEmail) => new()
        {
            RegisterToken = strRegisterToken,
            Email = strEmail,
            ExpiresIn = (int)_options.RegisterOtpTtl.TotalSeconds,
            ResendAfter = (int)_options.RegisterOtpResendCooldown.TotalSeconds
        };
    }
}
