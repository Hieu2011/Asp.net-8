using Shared.DataAccess.Abstractions;
using AuthService.Application.Contracts;
using AuthService.Application.Interfaces;
using AuthService.Domain;

namespace AuthService.Infrastructure.Postgres
{
    /// <inheritdoc cref="ISessionRepository" />
    public class SessionRepository : ISessionRepository
    {
        private readonly IDataCore _db;

        public SessionRepository(IDataCore db)
        {
            _db = db;
        }

        public async Task<Session> CreateAsync(Session session, CancellationToken cancellationToken = default)
        {
            _db.AddParameter("@user_id", session.UserId);
            _db.AddParameter("@device_info", session.DeviceInfo);
            _db.AddParameter("@ip_address", session.IpAddress);
            _db.AddParameter("@refresh_token_hash", session.RefreshTokenHash);
            _db.AddParameter("@expires_at", session.ExpiresAt);
            _db.AddParameter("@created_user", session.CreatedUser);

            return await _db.ExecStoreObjectFastAsync<Session>("sp_session_create", cancellationToken);
        }

        public async Task<Session?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _db.AddParameter("@id", id);
            var session = await _db.ExecStoreObjectFastAsync<Session>("sp_session_get_by_id", cancellationToken);
            return session.Id == Guid.Empty ? null : session;
        }

        public async Task<(Session Session, bool IsCurrentMatch)?> FindByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken cancellationToken = default)
        {
            _db.AddParameter("@hash", refreshTokenHash);
            var row = await _db.ExecStoreObjectFastAsync<SessionMatchRow>("sp_session_find_by_refresh_hash", cancellationToken);
            return row.Id == Guid.Empty ? null : (row, row.MatchType == "current");
        }

        public async Task<bool> RotateRefreshTokenAsync(Guid id, string newRefreshTokenHash, string updatedUser, CancellationToken cancellationToken = default)
        {
            _db.AddParameter("@id", id);
            _db.AddParameter("@new_refresh_token_hash", newRefreshTokenHash);
            _db.AddParameter("@updated_user", updatedUser);

            var result = await _db.ExecuteNonQueryAsStringAsync("sp_session_rotate_refresh_token", cancellationToken);
            return ParseBoolResult(result);
        }

        public async Task<List<Session>> GetActiveByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            _db.AddParameter("@user_id", userId);
            return await _db.ExecStoreToListObjectAsync<Session>("sp_session_get_active_by_user", cancellationToken);
        }

        public Task<List<SessionDto>> GetAllActiveAsync(CancellationToken cancellationToken = default)
            => _db.ExecStoreToListObjectAsync<SessionDto>("sp_session_get_all_active", cancellationToken);

        public async Task<bool> RevokeAsync(Guid id, string updatedUser, CancellationToken cancellationToken = default)
        {
            _db.AddParameter("@id", id);
            _db.AddParameter("@updated_user", updatedUser);

            var result = await _db.ExecuteNonQueryAsStringAsync("sp_session_revoke", cancellationToken);
            return ParseBoolResult(result);
        }

        public async Task<int> RevokeAllByUserAsync(Guid userId, string updatedUser, CancellationToken cancellationToken = default)
        {
            _db.AddParameter("@user_id", userId);
            _db.AddParameter("@updated_user", updatedUser);

            var result = await _db.ExecuteNonQueryAsStringAsync("sp_session_revoke_all_by_user", cancellationToken);
            return int.TryParse(result, out var count) ? count : 0;
        }

        private static bool ParseBoolResult(string result) => result.Trim() switch
        {
            "1" => true,
            "0" => false,
            _ => bool.TryParse(result, out var success) && success
        };

        /// <summary>Chỉ dùng nội bộ để map thêm cột match_type từ sp_session_find_by_refresh_hash (mục 6/20).</summary>
        private class SessionMatchRow : Session
        {
            public string MatchType { get; set; } = string.Empty;
        }
    }
}
