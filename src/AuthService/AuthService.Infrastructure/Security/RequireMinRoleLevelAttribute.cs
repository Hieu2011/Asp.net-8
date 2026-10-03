using Shared.Common.Contracts;
using AuthService.Application;
using AuthService.Application.Contracts;
using AuthService.Application.Interfaces;
using AuthService.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Infrastructure.Security
{
    /// <summary>
    /// Gate quyền theo ngưỡng tối thiểu (PLAN.md mục 2/6) — đọc claim "role_level" trong JWT
    /// (đã qua [Authorize]/JwtBearer xác thực chữ ký), so với IRoleAuthorizationService.CanAccess.
    /// Đây chỉ là cổng chặn THÔ (role tối thiểu); check "chỉ Admin"/self-target chi tiết hơn vẫn nằm
    /// trong Application Service (mục 5b) — 2 lớp phòng thủ, không thay thế nhau.
    /// </summary>
    public sealed class RequireMinRoleLevelAttribute : Attribute, IAuthorizationFilter
    {
        private readonly RoleLevel _requiredMin;

        public RequireMinRoleLevelAttribute(RoleLevel requiredMin)
        {
            _requiredMin = requiredMin;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            if (user.Identity?.IsAuthenticated != true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var roleClaim = user.FindFirst("role_level")?.Value;
            if (roleClaim is null || !int.TryParse(roleClaim, out var roleValue))
            {
                context.Result = new ObjectResult(APIResult.Error(AuthErrorCode.Forbidden, "Token thiếu claim role_level."))
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

            var actorRole = (RoleLevel)roleValue;
            var roleAuthorization = context.HttpContext.RequestServices.GetRequiredService<IRoleAuthorizationService>();

            if (!roleAuthorization.CanAccess(actorRole, _requiredMin))
            {
                context.Result = new ObjectResult(APIResult.Error(AuthErrorCode.Forbidden, "Không đủ quyền truy cập."))
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
    }
}
