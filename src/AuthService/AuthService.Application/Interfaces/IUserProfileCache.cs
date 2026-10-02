using AuthService.Application.Contracts;

namespace AuthService.Application.Interfaces
{
    /// <summary>
    /// Cache-aside cho GET /auth/me (mục 5b phát sinh) — tránh query Postgres mỗi lần UI muốn
    /// đọc lại profile (username/email/role...). TTL ngắn (Auth:UserProfileCacheTtl) tự hết hạn,
    /// KHÔNG cần invalidate ở mọi nơi user có thể đổi — chỉ invalidate chủ động ở vài thao tác
    /// Admin quan trọng (đổi role/disable/xóa) trong UserManagementService.
    /// </summary>
    public interface IUserProfileCache
    {
        Task<UserDto?> GetAsync(Guid userId, CancellationToken cancellationToken = default);
        Task SetAsync(Guid userId, UserDto profile, CancellationToken cancellationToken = default);
        Task RemoveAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
