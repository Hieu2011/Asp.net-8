using System.Security.Cryptography;
using System.Text;
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
            _options = options.Value;
        }

        public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, string remoteIp, CancellationToken cancellationToken = default)
        {
            if (!await _captchaVerifier.VerifyAsync(request.CaptchaToken, remoteIp, cancellationToken))
                throw new AuthDomainException(AuthErrorCode.CaptchaInvalid, "Xác thực chống bot không hợp lệ.");

            PasswordPolicy.Validate(request.Password);

            if (await _userRepository.GetByUsernameAsync(request.Username, cancellationToken) != null)
                throw new AuthDomainException(AuthErrorCode.UsernameAlreadyExists, "Username đã được sử dụng.");

            if (await _userRepository.GetByEmailAsync(request.Email, cancellationToken) != null)
                throw new AuthDomainException(AuthErrorCode.EmailAlreadyExists, "Email đã được sử dụng.");

            var totpSecret = _otpService.GenerateSecret();
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = _passwordHasher.Hash(request.Password),
                TotpSecret = _totpEncryption.Encrypt(totpSecret),
                RoleLevel = RoleLevel.Customer, // mục 7 — /auth/register công khai luôn ra cấp 5
                IsActive = true
            };

            var created = await _userRepository.CreateAsync(user, cancellationToken);
            return new RegisterResponse { OtpauthUri = _otpService.BuildOtpauthUri(totpSecret, created.Username) };
        }

        public async Task<TokenResponse> LoginAsync(LoginRequest request, string remoteIp, string? deviceInfo, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);

            // Mục 40 — check khóa TRƯỚC verify OTP, nhưng CHỈ SAU KHI xác nhận username tồn tại.
            if (user != null)
            {
                await AccountLockGuard.EnsureNotLockedAsync(user, _otpLockout, cancellationToken);

                // Mục 44 — bắt buộc captcha khi đã sai OTP >= 1 lần (adaptive, không làm phiền lần đầu)
                var otpFailCount = await _otpLockout.GetOtpFailCountAsync(user.Id, cancellationToken);
                if (otpFailCount >= 1 && !await _captchaVerifier.VerifyAsync(request.CaptchaToken ?? string.Empty, remoteIp, cancellationToken))
                    throw new AuthDomainException(AuthErrorCode.CaptchaRequired, "Cần xác thực chống bot.");
            }

            if (user == null || string.IsNullOrEmpty(user.TotpSecret))
            {
                await _loginHistory.InsertAsync(user?.Id, request.Username, remoteIp, deviceInfo, "otp", false, "invalid_credential", cancellationToken);
                throw new AuthDomainException(AuthErrorCode.InvalidCredential, "Sai thông tin đăng nhập.");
            }

            var decryptedSecret = _totpEncryption.Decrypt(user.TotpSecret);
            if (!_otpService.TryVerify(decryptedSecret, request.Otp, out var matchedTimeStep))
            {
                await _otpLockout.IncrementOtpFailAsync(user.Id, cancellationToken);
                await _loginHistory.InsertAsync(user.Id, request.Username, remoteIp, deviceInfo, "otp", false, "wrong_otp", cancellationToken);
                throw new AuthDomainException(AuthErrorCode.InvalidCredential, "Sai thông tin đăng nhập.");
            }

            // Mục 30 — chống replay cùng chu kỳ 30s
            if (!await _otpLockout.TryRecordOtpStepAsync(user.Id, matchedTimeStep, cancellationToken))
            {
                await _loginHistory.InsertAsync(user.Id, request.Username, remoteIp, deviceInfo, "otp", false, "otp_replay", cancellationToken);
                throw new AuthDomainException(AuthErrorCode.InvalidCredential, "Sai thông tin đăng nhập.");
            }

            await _otpLockout.ResetOtpFailAsync(user.Id, cancellationToken);
            await _loginHistory.InsertAsync(user.Id, request.Username, remoteIp, deviceInfo, "otp", true, null, cancellationToken);

            return await IssueTokenForUserAsync(user, remoteIp, deviceInfo, cancellationToken);
        }

        public async Task<TokenResponse> LoginWithPasswordAsync(LoginWithPasswordRequest request, string remoteIp, string? deviceInfo, CancellationToken cancellationToken = default)
        {
            // Mục 44 — luôn bắt buộc captcha ở fallback (khác /auth/login, vì đây đã là trạng thái nghi ngờ)
            if (!await _captchaVerifier.VerifyAsync(request.CaptchaToken, remoteIp, cancellationToken))
                throw new AuthDomainException(AuthErrorCode.CaptchaRequired, "Cần xác thực chống bot.");

            var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);

            if (user != null)
            {
                await AccountLockGuard.EnsureNotLockedAsync(user, _otpLockout, cancellationToken);

                // Mục 29 — gate: chỉ cho vào nếu đã sai OTP >= 3 HOẶC đang otp_locked_until.
                // Không có gate này thì OTP-first bị bypass hoàn toàn.
                var otpFailCount = await _otpLockout.GetOtpFailCountAsync(user.Id, cancellationToken);
                var isOtpLocked = await _otpLockout.IsOtpLockedAsync(user.Id, cancellationToken);
                if (otpFailCount < 3 && !isOtpLocked)
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

                await _loginHistory.InsertAsync(user?.Id, request.Username, remoteIp, deviceInfo, "password_fallback", false, "wrong_password", cancellationToken);
                throw new AuthDomainException(AuthErrorCode.InvalidCredential, "Sai thông tin đăng nhập.");
            }

            await _otpLockout.ResetPasswordFailAsync(user.Id, cancellationToken);
            await _otpLockout.ResetOtpFailAsync(user.Id, cancellationToken);
            await _loginHistory.InsertAsync(user.Id, request.Username, remoteIp, deviceInfo, "password_fallback", true, null, cancellationToken);

            return await IssueTokenForUserAsync(user, remoteIp, deviceInfo, cancellationToken);
        }

        public async Task<TokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            var refreshTokenHash = HashRefreshToken(refreshToken);
            var match = await _sessionRepository.FindByRefreshTokenHashAsync(refreshTokenHash, cancellationToken);
            if (match == null)
                throw new AuthDomainException(AuthErrorCode.RefreshTokenInvalid, "Refresh token không hợp lệ.");

            var (session, isCurrentMatch) = match.Value;

            if (!isCurrentMatch)
            {
                // Mục 20 — refresh token cũ (đã bị rotate) mà còn bị dùng lại: nghi bị đánh cắp,
                // revoke toàn bộ session của user này ngay.
                await _sessionRepository.RevokeAllByUserAsync(session.UserId, "system_reuse_detected", cancellationToken);
                throw new AuthDomainException(AuthErrorCode.RefreshTokenReuseDetected,
                    "Phát hiện refresh token bị dùng lại — toàn bộ phiên đăng nhập đã bị thu hồi.");
            }

            if (session.IsRevoked || session.ExpiresAt <= DateTime.UtcNow)
                throw new AuthDomainException(AuthErrorCode.RefreshTokenInvalid, "Phiên đăng nhập đã hết hạn hoặc bị thu hồi.");

            var user = await _userRepository.GetByIdAsync(session.UserId, cancellationToken);
            if (user == null || !user.IsActive || user.IsPermanentlyLocked)
                throw new AuthDomainException(AuthErrorCode.RefreshTokenInvalid, "Tài khoản không còn hiệu lực.");

            var newRefreshToken = GenerateRefreshToken();
            await _sessionRepository.RotateRefreshTokenAsync(session.Id, HashRefreshToken(newRefreshToken), user.Username, cancellationToken);

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

        private async Task<TokenResponse> IssueTokenForUserAsync(User user, string remoteIp, string? deviceInfo, CancellationToken cancellationToken)
        {
            var refreshToken = GenerateRefreshToken();
            var session = new Session
            {
                UserId = user.Id,
                DeviceInfo = deviceInfo,
                IpAddress = remoteIp,
                RefreshTokenHash = HashRefreshToken(refreshToken),
                LastActivityAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(_options.SessionAbsoluteTtl) // mục 21 — giới hạn tuyệt đối
            };

            var createdSession = await _sessionRepository.CreateAsync(session, cancellationToken);
            var accessToken = _jwtService.IssueAccessToken(user.Id, user.Username, user.RoleLevel, createdSession.Id, _options.AccessTokenTtl);

            return new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = (int)_options.AccessTokenTtl.TotalSeconds
            };
        }

        /// <summary>Random 256-bit, entropy cao sẵn — hash bằng SHA256 (mục 31), không BCrypt.</summary>
        private static string GenerateRefreshToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        private static string HashRefreshToken(string refreshToken)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
