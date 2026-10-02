namespace AuthService.Application.Interfaces
{
    /// <summary>
    /// Redis DÙNG CHUNG với Business API (ApiCore8) — ghi key revoked_session:{sessionId} mỗi khi
    /// 1 session bị revoke (logout/admin revoke/reuse detected...), để Business API tra cứu real-time
    /// mà không cần gọi đồng bộ sang AuthService mỗi request (mục 12). Implement ở Infrastructure.
    /// </summary>
    public interface ISessionRevocationCache
    {
        Task MarkRevokedAsync(Guid sessionId, TimeSpan ttl, CancellationToken cancellationToken = default);
    }
}
