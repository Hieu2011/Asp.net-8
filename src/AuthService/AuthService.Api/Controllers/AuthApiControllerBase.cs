using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthService.Application.Interfaces;
using AuthService.Domain;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers
{
    /// <summary>
    /// Cung cấp sẵn IP/device/claim của actor cho các Controller — tránh lặp lại logic đọc
    /// HttpContext ở từng Controller (mục 8 Bước 3, Controller mỏng).
    /// </summary>
    [ApiController]
    public abstract class AuthApiControllerBase : ControllerBase
    {
        private readonly IClientIpResolver _clientIpResolver;

        protected AuthApiControllerBase(IClientIpResolver clientIpResolver)
        {
            _clientIpResolver = clientIpResolver;
        }

        /// <summary>IP thật của caller (mục 24) — ưu tiên CF-Connecting-IP/X-Forwarded-For, chỉ tin khi RemoteIpAddress thuộc dải Cloudflare.</summary>
        protected string RemoteIp => _clientIpResolver.Resolve(
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            Request.Headers["CF-Connecting-IP"].FirstOrDefault(),
            Request.Headers["X-Forwarded-For"].FirstOrDefault());

        protected string? DeviceInfo => Request.Headers.UserAgent.ToString() is { Length: > 0 } ua ? ua : null;

        protected Guid CurrentUserId => Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

        protected string CurrentUsername => User.FindFirstValue("username") ?? string.Empty;

        protected RoleLevel CurrentRoleLevel => (RoleLevel)int.Parse(User.FindFirstValue("role_level")!);

        protected Guid CurrentSessionId => Guid.Parse(User.FindFirstValue("session_id")!);
    }
}
