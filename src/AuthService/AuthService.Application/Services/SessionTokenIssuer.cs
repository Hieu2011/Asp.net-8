using System.Security.Cryptography;
using System.Text;
using AuthService.Application.Contracts;
using AuthService.Application.Interfaces;
using AuthService.Application.Options;
using AuthService.Domain;
using Microsoft.Extensions.Options;

namespace AuthService.Application.Services
{
    /// <inheritdoc cref="ISessionTokenIssuer" />
    public sealed class SessionTokenIssuer : ISessionTokenIssuer
    {
        private readonly ISessionRepository _sessionRepository;
        private readonly IJwtService _jwtService;
        private readonly ISecurityNotifier _securityNotifier;
        private readonly AuthOptions _options;

        public SessionTokenIssuer(ISessionRepository sessionRepository, IJwtService jwtService, ISecurityNotifier securityNotifier, IOptions<AuthOptions> options)
        {
            _sessionRepository = sessionRepository;
            _jwtService = jwtService;
            _securityNotifier = securityNotifier;
            _options = options.Value;
        }

        public async Task<TokenResponse> IssueAsync(User user, string remoteIp, string? deviceInfo, string loginMethod, CancellationToken cancellationToken = default)
        {
            var strRefreshToken = GenerateRefreshToken();
            var objSession = new Session
            {
                UserId = user.Id,
                DeviceInfo = deviceInfo,
                IpAddress = remoteIp,
                RefreshTokenHash = HashRefreshToken(strRefreshToken),
                LastActivityAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(_options.SessionAbsoluteTtl) // mục 21 — giới hạn tuyệt đối
            };

            var objCreated = await _sessionRepository.CreateAsync(objSession, cancellationToken);
            var strAccessToken = _jwtService.IssueAccessToken(user.Id, user.Username, user.RoleLevel, objCreated.Id, _options.AccessTokenTtl);
            await _securityNotifier.NotifyLoginAsync(user, remoteIp, deviceInfo, loginMethod, cancellationToken);

            return new TokenResponse
            {
                AccessToken = strAccessToken,
                RefreshToken = strRefreshToken,
                ExpiresIn = (int)_options.AccessTokenTtl.TotalSeconds
            };
        }

        /// <summary>Random 256-bit, entropy cao sẵn — hash bằng SHA256 (mục 31), không BCrypt.</summary>
        internal static string GenerateRefreshToken()
            => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace('+', '-').Replace('/', '_').TrimEnd('=');

        internal static string HashRefreshToken(string refreshToken)
            => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken))).ToLowerInvariant();
    }
}
