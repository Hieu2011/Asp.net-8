namespace AuthService.Application.Interfaces
{
    /// <summary>Redis-backed — giới hạn số lần thử đăng nhập theo email (chặn botnet nhiều IP dồn vào 1 tài khoản).
    /// Bổ sung cho rate limit theo IP ở tầng Api.</summary>
    public interface ILoginEmailRateLimiter
    {
        /// <summary>Tính 1 lượt thử; null = còn được thử, có giá trị = vượt giới hạn, phải chờ bấy lâu.</summary>
        Task<TimeSpan?> TryAcquireAsync(string email, CancellationToken cancellationToken = default);
    }
}
