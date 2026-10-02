using AuthService.Application.Contracts;
using AuthService.Domain;

namespace AuthService.Application.Interfaces
{
    /// <summary>Application Service — Flow B, đối tác ngoài OAuth2 Client Credentials Grant (PLAN.md mục 5b/22-25).</summary>
    public interface IClientTokenService
    {
        Task<ClientTokenResponse> IssueClientTokenAsync(
            string clientId,
            string clientSecret,
            string remoteIpAddress,
            string? cfConnectingIp,
            string? xForwardedFor,
            CancellationToken cancellationToken = default);

        Task<RegenerateClientSecretResponse> RegenerateSecretAsync(RoleLevel actorRole, string actorUsername, Guid clientId, CancellationToken cancellationToken = default);

        Task DisableClientAsync(RoleLevel actorRole, string actorUsername, Guid clientId, CancellationToken cancellationToken = default);
    }
}
