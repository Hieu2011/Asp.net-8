using Shared.Common.Contracts;
using AuthService.Application.Contracts;
using AuthService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers
{
    /// <summary>Session của chính user đang đăng nhập (PLAN.md mục 7/33).</summary>
    [Route("auth/sessions")]
    [Authorize]
    public class SessionController : AuthApiControllerBase
    {
        private readonly ISessionManagementService _sessionService;

        public SessionController(ISessionManagementService sessionService, IClientIpResolver clientIpResolver)
            : base(clientIpResolver)
        {
            _sessionService = sessionService;
        }

        [HttpGet]
        public async Task<APIResult> GetMySessions(CancellationToken cancellationToken)
            => new(await _sessionService.GetMySessionsAsync(CurrentUserId, cancellationToken));

        [HttpDelete("{id:guid}")]
        public async Task<APIResult> RevokeSession(Guid id, CancellationToken cancellationToken)
        {
            await _sessionService.RevokeSessionAsync(CurrentUserId, id, cancellationToken);
            return new APIResult(new { Success = true });
        }

        [HttpDelete]
        public async Task<APIResult> RevokeAllMySessions(CancellationToken cancellationToken)
        {
            await _sessionService.RevokeAllMySessionsAsync(CurrentUserId, cancellationToken);
            return new APIResult(new { Success = true });
        }
    }
}
