namespace AuthService.Application.Interfaces
{
    /// <summary>Cloudflare Turnstile (mục 44). Implement ở Infrastructure bằng HttpClient thuần.</summary>
    public interface ICaptchaVerifier
    {
        Task<bool> VerifyAsync(string captchaToken, string? remoteIp, CancellationToken cancellationToken = default);
    }
}
