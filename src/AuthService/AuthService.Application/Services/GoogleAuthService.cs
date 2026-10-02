using System.Security.Cryptography;
using System.Linq;
using AuthService.Application.Contracts;
using AuthService.Application.Interfaces;
using AuthService.Domain;

namespace AuthService.Application.Services
{
    /// <inheritdoc cref="IGoogleAuthService" />
    public class GoogleAuthService : IGoogleAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IGoogleTokenVerifier _googleTokenVerifier;
        private readonly IGoogleSetupTokenService _googleSetupToken;
        private readonly IOtpLockoutService _otpLockout;
        private readonly IOtpService _otpService;
        private readonly ITotpSecretEncryption _totpEncryption;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ICaptchaVerifier _captchaVerifier;

        public GoogleAuthService(
            IUserRepository userRepository,
            IGoogleTokenVerifier googleTokenVerifier,
            IGoogleSetupTokenService googleSetupToken,
            IOtpLockoutService otpLockout,
            IOtpService otpService,
            ITotpSecretEncryption totpEncryption,
            IPasswordHasher passwordHasher,
            ICaptchaVerifier captchaVerifier)
        {
            _userRepository = userRepository;
            _googleTokenVerifier = googleTokenVerifier;
            _googleSetupToken = googleSetupToken;
            _otpLockout = otpLockout;
            _otpService = otpService;
            _totpEncryption = totpEncryption;
            _passwordHasher = passwordHasher;
            _captchaVerifier = captchaVerifier;
        }

        public async Task<GoogleLoginResponse> LoginWithGoogleAsync(GoogleLoginRequest request, CancellationToken cancellationToken = default)
        {
            var payload = await _googleTokenVerifier.VerifyAsync(request.IdToken, cancellationToken)
                ?? throw new AuthDomainException(AuthErrorCode.GoogleTokenInvalid, "Google ID Token không hợp lệ.");

            if (!payload.EmailVerified)
                throw new AuthDomainException(AuthErrorCode.GoogleEmailNotVerified, "Email Google chưa được xác thực.");

            var user = await _userRepository.GetByGoogleIdAsync(payload.Subject, cancellationToken)
                ?? await _userRepository.GetByEmailAsync(payload.Email, cancellationToken);

            if (user != null)
            {
                // Mục 46 — dù đã tồn tại, KHÔNG cấp token ở đây, check khóa y hệt mục 40 rồi bắt
                // client gọi tiếp /auth/login (username + OTP) như luồng thường.
                await AccountLockGuard.EnsureNotLockedAsync(user, _otpLockout, cancellationToken);

                if (user.GoogleId != payload.Subject)
                {
                    await _userRepository.LinkGoogleAsync(user.Id, payload.Subject, "system_google_login", cancellationToken);
                }

                return new GoogleLoginResponse { RequiresOtp = true, Username = user.Username };
            }

            // Email chưa tồn tại — tạo user mới, chưa có password/TOTP (mục 46)
            var username = await GenerateUniqueUsernameAsync(payload.Email, cancellationToken);
            var newUser = new User
            {
                Username = username,
                Email = payload.Email,
                GoogleId = payload.Subject,
                RoleLevel = RoleLevel.Customer,
                IsActive = true
            };

            var created = await _userRepository.CreateAsync(newUser, cancellationToken);
            var setupToken = await _googleSetupToken.IssueSetupTokenAsync(created.Id, cancellationToken);

            return new GoogleLoginResponse { RequiresSetup = true, SetupToken = setupToken };
        }

        public async Task<GoogleCompleteSetupResponse> CompleteSetupAsync(GoogleCompleteSetupRequest request, string remoteIp, CancellationToken cancellationToken = default)
        {
            if (!await _captchaVerifier.VerifyAsync(request.CaptchaToken, remoteIp, cancellationToken))
                throw new AuthDomainException(AuthErrorCode.CaptchaInvalid, "Xác thực chống bot không hợp lệ.");

            var userId = await _googleSetupToken.ConsumeSetupTokenAsync(request.SetupToken, cancellationToken)
                ?? throw new AuthDomainException(AuthErrorCode.GoogleSetupTokenInvalid, "Yêu cầu hoàn tất đăng ký không hợp lệ hoặc đã hết hạn.");

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
                ?? throw new AuthDomainException(AuthErrorCode.GoogleSetupTokenInvalid, "Tài khoản không tồn tại.");

            PasswordPolicy.Validate(request.NewPassword);

            await _userRepository.UpdatePasswordAsync(user.Id, _passwordHasher.Hash(request.NewPassword), "system_google_setup", cancellationToken);

            var totpSecret = _otpService.GenerateSecret();
            await _userRepository.UpdateTotpSecretAsync(user.Id, _totpEncryption.Encrypt(totpSecret), true, "system_google_setup", cancellationToken);

            return new GoogleCompleteSetupResponse { OtpauthUri = _otpService.BuildOtpauthUri(totpSecret, user.Username) };
        }

        /// <summary>Username không lấy trực tiếp từ email (không unique theo domain) — sinh từ local-part,
        /// thêm hậu tố số ngẫu nhiên nếu đã bị trùng.</summary>
        private async Task<string> GenerateUniqueUsernameAsync(string email, CancellationToken cancellationToken)
        {
            var localPart = email.Contains('@') ? email[..email.IndexOf('@')] : email;
            var basePart = new string(localPart.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
            if (string.IsNullOrEmpty(basePart)) basePart = "user";

            var candidate = basePart;
            for (var attempt = 0; attempt < 10; attempt++)
            {
                if (await _userRepository.GetByUsernameAsync(candidate, cancellationToken) == null)
                    return candidate;

                candidate = $"{basePart}{RandomNumberGenerator.GetInt32(1000, 9999)}";
            }

            throw new AuthDomainException(AuthErrorCode.UsernameAlreadyExists, "Không thể tạo username tự động, vui lòng thử lại.");
        }
    }
}
