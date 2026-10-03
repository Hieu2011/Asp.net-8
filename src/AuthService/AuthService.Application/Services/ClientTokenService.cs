using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using AuthService.Application.Contracts;
using AuthService.Application.Interfaces;
using AuthService.Application.Options;
using AuthService.Domain;
using Microsoft.Extensions.Options;

namespace AuthService.Application.Services
{
    /// <inheritdoc cref="IClientTokenService" />
    public class ClientTokenService : IClientTokenService
    {
        private readonly IClientRepository _clientRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtService _jwtService;
        private readonly IClientIpResolver _clientIpResolver;
        private readonly IClientRateLimiter _rateLimiter;
        private readonly IRoleAuthorizationService _roleAuthorization;
        private readonly AuthOptions _options;

        public ClientTokenService(
            IClientRepository clientRepository,
            IPasswordHasher passwordHasher,
            IJwtService jwtService,
            IClientIpResolver clientIpResolver,
            IClientRateLimiter rateLimiter,
            IRoleAuthorizationService roleAuthorization,
            IOptions<AuthOptions> options)
        {
            _clientRepository = clientRepository;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
            _clientIpResolver = clientIpResolver;
            _rateLimiter = rateLimiter;
            _roleAuthorization = roleAuthorization;
            _options = options.Value;
        }

        public async Task<ClientTokenResponse> IssueClientTokenAsync(
            string clientId,
            string clientSecret,
            string remoteIpAddress,
            string? cfConnectingIp,
            string? xForwardedFor,
            CancellationToken cancellationToken = default)
        {
            if (!await _rateLimiter.IsAllowedAsync(clientId, cancellationToken))
                throw new AuthDomainException(AuthErrorCode.ClientCredentialInvalid, "Quá nhiều yêu cầu, vui lòng thử lại sau.");

            var resolvedIp = _clientIpResolver.Resolve(remoteIpAddress, cfConnectingIp, xForwardedFor);
            await _rateLimiter.RecordAttemptAsync(clientId, resolvedIp, cancellationToken);

            var client = await _clientRepository.GetByClientIdAsync(clientId, cancellationToken);
            if (client == null || !client.IsActive || !_passwordHasher.Verify(clientSecret, client.ClientSecretHash))
                throw new AuthDomainException(AuthErrorCode.ClientCredentialInvalid, "client_id/client_secret không hợp lệ.");

            if (client.AllowedIps is { Count: > 0 } && !IsIpAllowed(resolvedIp, client.AllowedIps))
                throw new AuthDomainException(AuthErrorCode.ClientIpNotAllowed, "IP hiện tại không nằm trong danh sách được phép.");

            var accessToken = _jwtService.IssueClientAccessToken(client.ClientId, client.AllowedScopes, _options.AccessTokenTtl);
            return new ClientTokenResponse
            {
                AccessToken = accessToken,
                ExpiresIn = (int)_options.AccessTokenTtl.TotalSeconds
            };
        }

        public async Task<RegenerateClientSecretResponse> RegenerateSecretAsync(RoleLevel actorRole, string actorUsername, Guid clientId, CancellationToken cancellationToken = default)
        {
            EnsureAdmin(actorRole);

            // mục 25 — vô hiệu ngay, không grace period: hash mới ghi đè, secret cũ hết tác dụng tức thì.
            var newSecret = GenerateRandomSecret();
            await _clientRepository.RegenerateSecretAsync(clientId, _passwordHasher.Hash(newSecret), actorUsername, cancellationToken);
            return new RegenerateClientSecretResponse { ClientSecret = newSecret };
        }

        public async Task DisableClientAsync(RoleLevel actorRole, string actorUsername, Guid clientId, CancellationToken cancellationToken = default)
        {
            EnsureAdmin(actorRole);
            await _clientRepository.SetActiveAsync(clientId, false, actorUsername, cancellationToken);
        }

        private void EnsureAdmin(RoleLevel actorRole)
        {
            if (!_roleAuthorization.IsAdmin(actorRole))
                throw new AuthDomainException(AuthErrorCode.Forbidden, "Chỉ Admin mới được thực hiện thao tác này.");
        }

        private static string GenerateRandomSecret()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        /// <summary>Mục 23 — allowed_ips là danh sách CIDR/IP đơn, khớp 1 trong nhiều là được.</summary>
        private static bool IsIpAllowed(string ip, IReadOnlyList<string> allowedCidrs)
        {
            if (!IPAddress.TryParse(ip, out var address))
                return false;

            foreach (var cidr in allowedCidrs)
            {
                if (IsInCidrRange(address, cidr))
                    return true;
            }

            return false;
        }

        private static bool IsInCidrRange(IPAddress address, string cidr)
        {
            var parts = cidr.Split('/');
            if (!IPAddress.TryParse(parts[0], out var network))
                return false;

            var prefixLength = parts.Length > 1
                ? int.Parse(parts[1])
                : (network.AddressFamily == AddressFamily.InterNetwork ? 32 : 128);

            var addressBytes = address.GetAddressBytes();
            var networkBytes = network.GetAddressBytes();
            if (addressBytes.Length != networkBytes.Length)
                return false;

            var fullBytes = prefixLength / 8;
            var remainingBits = prefixLength % 8;

            for (var i = 0; i < fullBytes; i++)
            {
                if (addressBytes[i] != networkBytes[i])
                    return false;
            }

            if (remainingBits > 0)
            {
                var mask = (byte)~(0xFF >> remainingBits);
                if ((addressBytes[fullBytes] & mask) != (networkBytes[fullBytes] & mask))
                    return false;
            }

            return true;
        }
    }
}
