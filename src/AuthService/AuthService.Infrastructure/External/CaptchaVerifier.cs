using System.Text.Json;
using AuthService.Application.Interfaces;
using AuthService.Application.Options;
using Microsoft.Extensions.Options;

namespace AuthService.Infrastructure.External
{
    /// <inheritdoc cref="ICaptchaVerifier" />
    public class CaptchaVerifier : ICaptchaVerifier
    {
        private const string VerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

        private readonly HttpClient _httpClient;
        private readonly TurnstileOptions _options;

        public CaptchaVerifier(HttpClient httpClient, IOptions<TurnstileOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<bool> VerifyAsync(string captchaToken, string? remoteIp, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(captchaToken))
                return false;

            var form = new Dictionary<string, string>
            {
                ["secret"] = _options.SecretKey,
                ["response"] = captchaToken
            };
            if (!string.IsNullOrWhiteSpace(remoteIp))
                form["remoteip"] = remoteIp;

            using var response = await _httpClient.PostAsync(VerifyUrl, new FormUrlEncodedContent(form), cancellationToken);
            if (!response.IsSuccessStatusCode)
                return false;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("success", out var successProp) && successProp.GetBoolean();
        }
    }
}
