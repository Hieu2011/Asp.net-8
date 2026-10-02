using Shared.Common.Contracts;
using AuthService.Application.Contracts;
using AuthService.Application.Interfaces;
using AuthService.Domain;
using AuthService.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers
{
    /// <summary>Quản lý đối tác Flow B — chỉ Admin (PLAN.md mục 7/25).</summary>
    [Route("clients")]
    [Authorize]
    [RequireMinRoleLevel(RoleLevel.Admin)]
    public class ClientController : AuthApiControllerBase
    {
        private readonly IClientTokenService _clientTokenService;

        public ClientController(IClientTokenService clientTokenService, IClientIpResolver clientIpResolver)
            : base(clientIpResolver)
        {
            _clientTokenService = clientTokenService;
        }

        [HttpPut("{id:guid}/regenerate-secret")]
        public async Task<APIResult> RegenerateSecret(Guid id, CancellationToken cancellationToken)
            => new(await _clientTokenService.RegenerateSecretAsync(CurrentRoleLevel, CurrentUsername, id, cancellationToken));

        [HttpPut("{id:guid}/disable")]
        public async Task<APIResult> DisableClient(Guid id, CancellationToken cancellationToken)
        {
            await _clientTokenService.DisableClientAsync(CurrentRoleLevel, CurrentUsername, id, cancellationToken);
            return new APIResult(new { Success = true });
        }
    }
}
