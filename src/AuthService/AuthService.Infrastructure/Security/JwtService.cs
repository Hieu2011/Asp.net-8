using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthService.Application.Interfaces;
using AuthService.Application.Options;
using AuthService.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Infrastructure.Security
{
    /// <inheritdoc cref="IJwtService" />
    /// <remarks>
    /// Ký RSA, ép RS256 tường minh qua <see cref="SecurityAlgorithms.RsaSha256"/> (mục 15 — chặn
    /// algorithm confusion phải được validate ở PHÍA NHẬN, tức ApiCore8.Api, không phải ở đây).
    /// Khóa lấy từ <see cref="RsaSigningKeyRing"/> (header <c>kid</c> để bên nhận chọn đúng khóa qua JWKS).
    /// Token user và token đối tác khác <c>aud</c> + <c>token_use</c> → bên nhận không nhận nhầm loại.
    /// </remarks>
    public class JwtService : IJwtService
    {
        public const string TokenUseUser = "user";
        public const string TokenUseClient = "client";

        private readonly RsaSigningKeyRing _keyRing;
        private readonly AuthOptions _options;

        public JwtService(RsaSigningKeyRing keyRing, IOptions<AuthOptions> options)
        {
            _keyRing = keyRing;
            _options = options.Value;
        }

        public string IssueAccessToken(Guid userId, string username, RoleLevel roleLevel, Guid sessionId, TimeSpan ttl)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim("username", username),
                new Claim("role_level", ((int)roleLevel).ToString()),
                new Claim("session_id", sessionId.ToString()),
                new Claim("token_use", TokenUseUser)
            };

            return WriteToken(claims, _options.UserAudience, ttl);
        }

        public string IssueClientAccessToken(string clientId, IReadOnlyList<string> scopes, TimeSpan ttl)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, clientId),
                new Claim("client_id", clientId),
                new Claim("scopes", string.Join(' ', scopes)),
                new Claim("token_use", TokenUseClient)
            };

            return WriteToken(claims, _options.PartnerAudience, ttl);
        }

        private string WriteToken(IEnumerable<Claim> claims, string strAudience, TimeSpan ttl)
        {
            var now = DateTime.UtcNow;
            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: strAudience,
                claims: claims.Append(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))),
                notBefore: now,
                expires: now.Add(ttl),
                signingCredentials: _keyRing.SigningCredentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
