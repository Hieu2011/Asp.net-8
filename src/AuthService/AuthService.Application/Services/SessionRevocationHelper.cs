using AuthService.Application.Interfaces;

namespace AuthService.Application.Services
{
    /// <summary>
    /// Dùng chung cho mọi nơi cần "revoke TẤT CẢ session của 1 user" (mục 33/34/42, disable/delete
    /// user) — revoke ở DB (store trả đúng danh sách id vừa revoke) rồi đánh dấu từng session trong Redis
    /// (mục 12) để Business API biết ngay mà không cần chờ TTL riêng của mỗi session.
    /// </summary>
    internal static class SessionRevocationHelper
    {
        public static async Task<int> RevokeAllAndMarkCacheAsync(
            ISessionRepository sessionRepository,
            ISessionRevocationCache revocationCache,
            Guid userId,
            string actorUsername,
            TimeSpan cacheTtl,
            CancellationToken cancellationToken)
        {
            var lstRevokedID = await sessionRepository.RevokeAllByUserAsync(userId, actorUsername, cancellationToken);

            foreach (var guidSessionID in lstRevokedID)
            {
                await revocationCache.MarkRevokedAsync(guidSessionID, cacheTtl, cancellationToken);
            }

            return lstRevokedID.Count;
        }
    }
}
