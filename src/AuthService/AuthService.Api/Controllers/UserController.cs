using Shared.Common.Contracts;
using AuthService.Application.Contracts;
using AuthService.Application.Interfaces;
using AuthService.Domain;
using AuthService.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers
{
    /// <summary>Quản lý user + session của user khác (PLAN.md mục 7/14) — mỗi action gắn đúng
    /// ngưỡng quyền theo bảng endpoint; check "chỉ Admin"/self-target chi tiết hơn nằm trong Service.</summary>
    [Route("auth")]
    [Authorize]
    public class UserController : AuthApiControllerBase
    {
        private readonly IUserManagementService _userManagementService;
        private readonly ISessionManagementService _sessionService;

        public UserController(
            IUserManagementService userManagementService,
            ISessionManagementService sessionService,
            IClientIpResolver clientIpResolver)
            : base(clientIpResolver)
        {
            _userManagementService = userManagementService;
            _sessionService = sessionService;
        }

        [RequireMinRoleLevel(RoleLevel.Manager)]
        [HttpGet("users")]
        public async Task<APIResult> GetUsers([FromQuery] RoleLevel? roleLevel, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
            => new(await _userManagementService.GetUsersAsync(roleLevel, page, pageSize, cancellationToken));

        [RequireMinRoleLevel(RoleLevel.Admin)]
        [HttpPut("users/{id:guid}/role")]
        public async Task<APIResult> ChangeRole(Guid id, [FromBody] ChangeUserRoleRequest request, CancellationToken cancellationToken)
        {
            await _userManagementService.ChangeRoleAsync(CurrentUserId, CurrentRoleLevel, CurrentUsername, id, request.NewRoleLevel, cancellationToken);
            return new APIResult(new { Success = true });
        }

        [RequireMinRoleLevel(RoleLevel.Admin)]
        [HttpPut("users/{id:guid}/disable")]
        public async Task<APIResult> DisableUser(Guid id, CancellationToken cancellationToken)
        {
            await _userManagementService.DisableAsync(CurrentUserId, CurrentRoleLevel, CurrentUsername, id, cancellationToken);
            return new APIResult(new { Success = true });
        }

        [RequireMinRoleLevel(RoleLevel.Admin)]
        [HttpDelete("users/{id:guid}")]
        public async Task<APIResult> DeleteUser(Guid id, CancellationToken cancellationToken)
        {
            await _userManagementService.DeleteAsync(CurrentUserId, CurrentRoleLevel, CurrentUsername, id, cancellationToken);
            return new APIResult(new { Success = true });
        }

        [RequireMinRoleLevel(RoleLevel.Admin)]
        [HttpPut("users/{id:guid}/unlock")]
        public async Task<APIResult> UnlockUser(Guid id, CancellationToken cancellationToken)
        {
            await _userManagementService.UnlockAsync(CurrentUsername, id, cancellationToken);
            return new APIResult(new { Success = true });
        }

        [RequireMinRoleLevel(RoleLevel.Manager)]
        [HttpGet("admin/sessions")]
        public async Task<APIResult> GetAllActiveSessions(CancellationToken cancellationToken)
            => new(await _sessionService.GetAllActiveSessionsAsync(cancellationToken));

        [RequireMinRoleLevel(RoleLevel.Manager)]
        [HttpGet("users/{id:guid}/sessions")]
        public async Task<APIResult> GetUserSessions(Guid id, CancellationToken cancellationToken)
            => new(await _sessionService.GetUserSessionsAsync(id, cancellationToken));

        [RequireMinRoleLevel(RoleLevel.Admin)]
        [HttpDelete("users/{id:guid}/sessions")]
        public async Task<APIResult> RevokeUserSessions(Guid id, CancellationToken cancellationToken)
        {
            await _sessionService.RevokeUserSessionsAsync(CurrentRoleLevel, CurrentUsername, id, cancellationToken);
            return new APIResult(new { Success = true });
        }

        [RequireMinRoleLevel(RoleLevel.Admin)]
        [HttpDelete("users/{id:guid}/sessions/{sessionId:guid}")]
        public async Task<APIResult> RevokeUserSession(Guid id, Guid sessionId, CancellationToken cancellationToken)
        {
            await _sessionService.RevokeUserSessionAsync(CurrentRoleLevel, CurrentUsername, id, sessionId, cancellationToken);
            return new APIResult(new { Success = true });
        }
    }
}
