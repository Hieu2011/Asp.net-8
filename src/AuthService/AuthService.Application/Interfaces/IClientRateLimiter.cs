namespace AuthService.Application.Interfaces
{
    /// <summary>Redis-backed — rate limit + audit theo client_id cho /auth/token Flow B (mục 25). Implement ở Infrastructure.</summary>
    public interface IClientRateLimiter
    {
        Task<bool> IsAllowedAsync(string clientId, CancellationToken cancellationToken = default);
        Task RecordAttemptAsync(string clientId, string? ipAddress, CancellationToken cancellationToken = default);
    }
}
