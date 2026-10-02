using Shared.Common.Contracts;
using System.Net.Http.Headers;
using System.Text;
using AuthService.Application;
using AuthService.Application.Contracts;
using AuthService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers
{
    /// <summary>Flow B — OAuth2 Client Credentials Grant cho đối tác ngoài (PLAN.md mục 7/22).
    /// client_id/client_secret đi qua header Authorization: Basic, KHÔNG qua [Authorize]/JWT vì
    /// đây chính là endpoint CẤP token, không phải endpoint cần token.</summary>
    [Route("auth/token")]
    public class TokenController : ControllerBase
    {
        private readonly IClientTokenService _clientTokenService;

        public TokenController(IClientTokenService clientTokenService)
        {
            _clientTokenService = clientTokenService;
        }

        [HttpPost]
        public async Task<APIResult> IssueToken([FromForm] ClientTokenRequest request, CancellationToken cancellationToken)
        {
            if (!TryParseBasicAuth(Request.Headers.Authorization, out var clientId, out var clientSecret))
                throw new AuthDomainException(AuthErrorCode.ClientCredentialInvalid, "Thiếu hoặc sai định dạng header Authorization: Basic.");

            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
            var cfConnectingIp = Request.Headers["CF-Connecting-IP"].FirstOrDefault();
            var xForwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();

            var result = await _clientTokenService.IssueClientTokenAsync(clientId, clientSecret, remoteIp, cfConnectingIp, xForwardedFor, cancellationToken);
            return new APIResult(result);
        }

        private static bool TryParseBasicAuth(string? authorizationHeader, out string clientId, out string clientSecret)
        {
            clientId = string.Empty;
            clientSecret = string.Empty;

            if (!AuthenticationHeaderValue.TryParse(authorizationHeader, out var headerValue)
                || !string.Equals(headerValue.Scheme, "Basic", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrEmpty(headerValue.Parameter))
            {
                return false;
            }

            string decoded;
            try
            {
                decoded = Encoding.UTF8.GetString(Convert.FromBase64String(headerValue.Parameter));
            }
            catch (FormatException)
            {
                return false;
            }

            var separatorIndex = decoded.IndexOf(':');
            if (separatorIndex < 0)
                return false;

            clientId = decoded[..separatorIndex];
            clientSecret = decoded[(separatorIndex + 1)..];
            return !string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret);
        }
    }
}
