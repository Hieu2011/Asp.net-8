using System.Security.Cryptography;
using System.Text;
using AuthService.Application.Contracts;
using AuthService.Application.Interfaces;
using AuthService.Application.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Infrastructure.Security
{
    /// <summary>
    /// Bộ khóa JWT (Singleton — import RSA 1 lần lúc start): khóa ký hiện tại + public key các khóa cũ còn chấp nhận.
    /// Xoay khóa: đổi <c>Auth:RsaPrivateKey</c>, đưa public key cũ vào <c>Auth:RsaPreviousPublicKeys</c> → token cũ
    /// vẫn verify được tới khi hết hạn, không ai bị đăng xuất.
    /// </summary>
    public sealed class RsaSigningKeyRing : IJwksProvider
    {
        private readonly JwksResponse _objJwks;

        public RsaSigningKeyRing(IOptions<AuthOptions> options)
        {
            var rsaSigning = RSA.Create();
            rsaSigning.ImportFromPem(options.Value.RsaPrivateKey);
            var objSigningPublicKey = ToPublicKey(rsaSigning);
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(rsaSigning) { KeyId = objSigningPublicKey.KeyId }, SecurityAlgorithms.RsaSha256);

            // Verify chỉ cần public key — private key chỉ dùng để ký
            var lstValidationKey = new List<RsaSecurityKey> { objSigningPublicKey };
            foreach (var strPem in options.Value.RsaPreviousPublicKeys)
            {
                using var rsaPrevious = RSA.Create();
                rsaPrevious.ImportFromPem(strPem);
                lstValidationKey.Add(ToPublicKey(rsaPrevious));
            }

            ValidationKeys = lstValidationKey;
            _objJwks = new JwksResponse { Keys = lstValidationKey.Select(ToJwk).ToList() };
        }

        public SigningCredentials SigningCredentials { get; }

        public IReadOnlyList<SecurityKey> ValidationKeys { get; }

        public JwksResponse GetJwks() => _objJwks;

        private static RsaSecurityKey ToPublicKey(RSA rsa)
        {
            var objParameters = rsa.ExportParameters(false);
            return new RsaSecurityKey(objParameters) { KeyId = ComputeKeyId(objParameters) };
        }

        // kid = JWK thumbprint (RFC 7638) — cố định theo public key, 2 bên tính ra giống nhau
        private static string ComputeKeyId(RSAParameters objParameters)
        {
            var strCanonical = $$"""{"e":"{{Base64UrlEncoder.Encode(objParameters.Exponent)}}","kty":"RSA","n":"{{Base64UrlEncoder.Encode(objParameters.Modulus)}}"}""";
            return Base64UrlEncoder.Encode(SHA256.HashData(Encoding.UTF8.GetBytes(strCanonical)));
        }

        private static JwkDto ToJwk(RsaSecurityKey objKey) => new()
        {
            Kid = objKey.KeyId,
            N = Base64UrlEncoder.Encode(objKey.Parameters.Modulus),
            E = Base64UrlEncoder.Encode(objKey.Parameters.Exponent)
        };
    }
}
