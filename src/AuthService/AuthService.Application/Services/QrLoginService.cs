using AuthService.Application.Contracts;
using AuthService.Application.Interfaces;
using AuthService.Application.Options;
using AuthService.Domain;
using Microsoft.Extensions.Options;

namespace AuthService.Application.Services
{
    /// <inheritdoc cref="IQrLoginService" />
    public sealed class QrLoginService : IQrLoginService
    {
        private const int PollIntervalSeconds = 2;

        private readonly IQrLoginStore _qrStore;
        private readonly IUserRepository _userRepository;
        private readonly IOtpLockoutService _otpLockout;
        private readonly ISessionTokenIssuer _tokenIssuer;
        private readonly ILoginHistoryRepository _loginHistory;
        private readonly AuthOptions _options;

        public QrLoginService(
            IQrLoginStore qrStore,
            IUserRepository userRepository,
            IOtpLockoutService otpLockout,
            ISessionTokenIssuer tokenIssuer,
            ILoginHistoryRepository loginHistory,
            IOptions<AuthOptions> options)
        {
            _qrStore = qrStore;
            _userRepository = userRepository;
            _otpLockout = otpLockout;
            _tokenIssuer = tokenIssuer;
            _loginHistory = loginHistory;
            _options = options.Value;
        }

        public async Task<QrStartResponse> StartAsync(string remoteIp, string? deviceInfo, CancellationToken cancellationToken = default)
        {
            var (strChallengeId, strPollSecret) = await _qrStore.CreateAsync(remoteIp, deviceInfo, cancellationToken);
            return new QrStartResponse
            {
                ChallengeId = strChallengeId,
                PollSecret = strPollSecret,
                ExpiresIn = (int)_options.QrLoginTtl.TotalSeconds,
                PollInterval = PollIntervalSeconds
            };
        }

        public async Task<QrChallengeInfo> ScanAsync(string challengeId, CancellationToken cancellationToken = default)
            => await _qrStore.ScanAsync(challengeId, cancellationToken) ?? throw QrInvalid();

        public async Task ApproveAsync(string challengeId, Guid userId, CancellationToken cancellationToken = default)
        {
            // Duyệt bằng tài khoản đang bị khóa/vô hiệu thì không cho — tránh dùng phiên cũ trên điện thoại để vượt khóa
            await EnsureUserCanSignInAsync(userId, cancellationToken);
            if (!await _qrStore.DecideAsync(challengeId, true, userId, cancellationToken))
                throw QrInvalid();
        }

        public async Task RejectAsync(string challengeId, Guid userId, CancellationToken cancellationToken = default)
        {
            if (!await _qrStore.DecideAsync(challengeId, false, userId, cancellationToken))
                throw QrInvalid();
        }

        public async Task<QrPollResponse> PollAsync(QrPollRequest request, string remoteIp, string? deviceInfo, CancellationToken cancellationToken = default)
        {
            var (strStatus, guidUserId) = await _qrStore.PollAsync(request.ChallengeId, request.PollSecret, cancellationToken);
            if (strStatus != QrLoginStatus.Approved || guidUserId == null)
                return new QrPollResponse { Status = strStatus };

            var objUser = await EnsureUserCanSignInAsync(guidUserId.Value, cancellationToken);
            await _loginHistory.InsertAsync(objUser.Id, objUser.Email, remoteIp, deviceInfo, "qr", true, null, cancellationToken);
            return new QrPollResponse
            {
                Status = QrLoginStatus.Approved,
                Token = await _tokenIssuer.IssueAsync(objUser, remoteIp, deviceInfo, LoginMethods.Qr, cancellationToken)
            };
        }

        private async Task<User> EnsureUserCanSignInAsync(Guid guidUserId, CancellationToken cancellationToken)
        {
            var objUser = await _userRepository.GetByIdAsync(guidUserId, cancellationToken);
            if (objUser == null || !objUser.IsActive)
                throw new AuthDomainException(AuthErrorCode.Forbidden, "Tài khoản không còn hiệu lực.");
            await AccountLockGuard.EnsureNotLockedAsync(objUser, _otpLockout, cancellationToken);
            return objUser;
        }

        private static AuthDomainException QrInvalid()
            => new(AuthErrorCode.QrChallengeInvalid, "Mã QR đã hết hạn hoặc đã được sử dụng, vui lòng tạo mã mới trên máy tính.");
    }
}
