using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
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
    /// Đăng ký Singleton (mục DI) vì import RSA key tốn chi phí, chỉ cần làm 1 lần lúc start-up.
    /// </remarks>
    public class JwtService : IJwtService
    {
        private readonly SigningCredentials _signingCredentials;

        public JwtService(IOptions<AuthOptions> options)
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(options.Value.RsaPrivateKey);
            _signingCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);
        }

        public string IssueAccessToken(Guid userId, string username, RoleLevel roleLevel, Guid sessionId, TimeSpan ttl)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim("username", username),
                new Claim("role_level", ((int)roleLevel).ToString()),
                new Claim("session_id", sessionId.ToString())
            };

            return WriteToken(claims, ttl);
        }

        public string IssueClientAccessToken(string clientId, IReadOnlyList<string> scopes, TimeSpan ttl)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, clientId),
                new Claim("client_id", clientId),
                new Claim("scopes", string.Join(' ', scopes))
            };

            return WriteToken(claims, ttl);
        }

        private string WriteToken(IEnumerable<Claim> claims, TimeSpan ttl)
        {
            var now = DateTime.UtcNow;
            var token = new JwtSecurityToken(
                claims: claims,
                notBefore: now,
                expires: now.Add(ttl),
                signingCredentials: _signingCredentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
