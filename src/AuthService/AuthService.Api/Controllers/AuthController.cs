using Shared.Common.Contracts;
using AuthService.Application.Contracts;
using AuthService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers
{
    /// <summary>Đăng ký/đăng nhập/quên mật khẩu/Google Sign-In (PLAN.md mục 7) — Controller chỉ gọi
    /// Application Service rồi wrap APIResult, lỗi nghiệp vụ do AuthDomainExceptionFilter xử lý chung.</summary>
    [Route("auth")]
    public class AuthController : AuthApiControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IPasswordRecoveryService _passwordRecoveryService;
        private readonly IGoogleAuthService _googleAuthService;
        private readonly IUserManagementService _userManagementService;

        public AuthController(
            IAuthService authService,
            IPasswordRecoveryService passwordRecoveryService,
            IGoogleAuthService googleAuthService,
            IUserManagementService userManagementService,
            IClientIpResolver clientIpResolver)
            : base(clientIpResolver)
        {
            _authService = authService;
            _passwordRecoveryService = passwordRecoveryService;
            _googleAuthService = googleAuthService;
            _userManagementService = userManagementService;
        }

        [HttpPost("register")]
        public async Task<APIResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
            => new(await _authService.RegisterAsync(request, RemoteIp, cancellationToken));

        [HttpPost("login")]
        public async Task<APIResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
            => new(await _authService.LoginAsync(request, RemoteIp, DeviceInfo, cancellationToken));

        [HttpPost("login/password")]
        public async Task<APIResult> LoginWithPassword([FromBody] LoginWithPasswordRequest request, CancellationToken cancellationToken)
            => new(await _authService.LoginWithPasswordAsync(request, RemoteIp, DeviceInfo, cancellationToken));

        [HttpPost("login/google")]
        public async Task<APIResult> LoginWithGoogle([FromBody] GoogleLoginRequest request, CancellationToken cancellationToken)
            => new(await _googleAuthService.LoginWithGoogleAsync(request, cancellationToken));

        [HttpPost("register/google/complete")]
        public async Task<APIResult> CompleteGoogleSetup([FromBody] GoogleCompleteSetupRequest request, CancellationToken cancellationToken)
            => new(await _googleAuthService.CompleteSetupAsync(request, RemoteIp, cancellationToken));

        [HttpPost("password/forgot")]
        public async Task<APIResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
        {
            // Mục 39 — response luôn generic dù username tồn tại hay không, tránh lộ oracle username.
            await _passwordRecoveryService.ForgotPasswordAsync(request, RemoteIp, cancellationToken);
            return new APIResult(new { Message = "Nếu tài khoản tồn tại, mã OTP đã được gửi qua email đã đăng ký." });
        }

        [HttpPost("password/verify-reset-otp")]
        public async Task<APIResult> VerifyResetOtp([FromBody] VerifyResetOtpRequest request, CancellationToken cancellationToken)
            => new(await _passwordRecoveryService.VerifyResetOtpAsync(request, cancellationToken));

        [HttpPost("password/reset")]
        public async Task<APIResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
            => new(await _passwordRecoveryService.ResetPasswordAsync(request, RemoteIp, cancellationToken));

        [HttpPost("token/refresh")]
        public async Task<APIResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
            => new(await _authService.RefreshTokenAsync(request.RefreshToken, cancellationToken));

        [Authorize]
        [HttpGet("me")]
        public async Task<APIResult> GetMyProfile(CancellationToken cancellationToken)
            => new(await _userManagementService.GetMyProfileAsync(CurrentUserId, cancellationToken));

        [Authorize]
        [HttpPost("logout")]
        public async Task<APIResult> Logout(CancellationToken cancellationToken)
        {
            await _authService.LogoutAsync(CurrentSessionId, CurrentUsername, cancellationToken);
            return new APIResult(new { Message = "Đăng xuất thành công." });
        }
    }
}
