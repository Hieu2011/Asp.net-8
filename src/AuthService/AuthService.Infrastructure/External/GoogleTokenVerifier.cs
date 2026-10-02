using AuthService.Application.Interfaces;
using AuthService.Application.Options;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;

namespace AuthService.Infrastructure.External
{
    /// <inheritdoc cref="IGoogleTokenVerifier" />
    public class GoogleTokenVerifier : IGoogleTokenVerifier
    {
        private readonly GoogleOptions _options;

        public GoogleTokenVerifier(IOptions<GoogleOptions> options)
        {
            _options = options.Value;
        }

        public async Task<GoogleTokenPayload?> VerifyAsync(string idToken, CancellationToken cancellationToken = default)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _options.ClientId }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
                return new GoogleTokenPayload
                {
                    Subject = payload.Subject,
                    Email = payload.Email,
                    EmailVerified = payload.EmailVerified
                };
            }
            catch (InvalidJwtException)
            {
                // Chữ ký/aud/iss/hết hạn sai — coi như token không hợp lệ, không ném lỗi ra ngoài.
                return null;
            }
        }
    }
}
