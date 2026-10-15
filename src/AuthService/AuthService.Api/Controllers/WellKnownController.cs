using AuthService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers
{
    /// <summary>Endpoint chuẩn cho bên nhận JWT (ApiCore8) lấy public key — chọn khóa theo <c>kid</c>, tự nhận khóa mới khi xoay.</summary>
    [ApiController]
    [AllowAnonymous]
    [Route(".well-known")]
    public sealed class WellKnownController : ControllerBase
    {
        private readonly IJwksProvider _jwksProvider;

        public WellKnownController(IJwksProvider jwksProvider)
        {
            _jwksProvider = jwksProvider;
        }

        /// <summary>JWKS (RFC 7517). Ngoại lệ không bọc APIResult — thư viện JwtBearer bên nhận đọc thẳng định dạng chuẩn <c>{"keys":[...]}</c>.</summary>
        [HttpGet("jwks.json")]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
        public IActionResult GetJwks() => Ok(_jwksProvider.GetJwks());
    }
}
