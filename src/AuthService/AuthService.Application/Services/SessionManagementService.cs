using AuthService.Application.Contracts;
using AuthService.Application.Interfaces;
using AuthService.Application.Options;
using AuthService.Domain;
using Microsoft.Extensions.Options;

namespace AuthService.Application.Services
{
    /// <inheritdoc cref="ISessionManagementService" />
    public class SessionManagementService : ISessionManagementService
    {
        private readonly ISessionRepository _sessionRepository;
        private readonly ISessionRevocationCache _revocationCache;
        private readonly IRoleAuthorizationService _roleAuthorization;
        private readonly AuthOptions _options;

        public SessionManagementService(
            ISessionRepository sessionRepository,
            ISessionRevocationCache revocationCache,
            IRoleAuthorizationService roleAuthorization,
            IOptions<AuthOptions> options)
        {
            _sessionRepository = sessionRepository;
            _revocationCache = revocationCache;
            _roleAuthorization = roleAuthorization;
            _options = options.Value;
        }

        public async Task<List<SessionDto>> GetMySessionsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var sessions = await _sessionRepository.GetActiveByUserAsync(userId, cancellationToken);
            return sessions.Select(s => MapToDto(s, username: null)).ToList();
        }

        public async Task RevokeSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default)
        {
            var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);

            // Chặn IDOR — session phải thuộc đúng user đang gọi, không được đoán ID người khác.
            if (session == null || session.UserId != userId)
                throw new AuthDomainException(AuthErrorCode.SessionNotFound, "Không tìm thấy session.");

            await _sessionRepository.RevokeAsync(sessionId, "self", cancellationToken);
            await _revocationCache.MarkRevokedAsync(sessionId, _options.SessionAbsoluteTtl, cancellationToken);
        }

        public async Task RevokeAllMySessionsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            await SessionRevocationHelper.RevokeAllAndMarkCacheAsync(
                _sessionRepository, _revocationCache, userId, "self", _options.SessionAbsoluteTtl, cancellationToken);
        }

        public Task<List<SessionDto>> GetAllActiveSessionsAsync(CancellationToken cancellationToken = default)
            => _sessionRepository.GetAllActiveAsync(cancellationToken);

        public async Task<List<SessionDto>> GetUserSessionsAsync(Guid targetUserId, CancellationToken cancellationToken = default)
        {
            var sessions = await _sessionRepository.GetActiveByUserAsync(targetUserId, cancellationToken);
            return sessions.Select(s => MapToDto(s, username: null)).ToList();
        }

        public async Task RevokeUserSessionsAsync(RoleLevel actorRole, string actorUsername, Guid targetUserId, CancellationToken cancellationToken = default)
        {
            EnsureAdmin(actorRole);
            await SessionRevocationHelper.RevokeAllAndMarkCacheAsync(
                _sessionRepository, _revocationCache, targetUserId, actorUsername, _options.SessionAbsoluteTtl, cancellationToken);
        }

        public async Task RevokeUserSessionAsync(RoleLevel actorRole, string actorUsername, Guid targetUserId, Guid sessionId, CancellationToken cancellationToken = default)
        {
            EnsureAdmin(actorRole);

            var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
            if (session == null || session.UserId != targetUserId)
                throw new AuthDomainException(AuthErrorCode.SessionNotFound, "Không tìm thấy session.");

            await _sessionRepository.RevokeAsync(sessionId, actorUsername, cancellationToken);
            await _revocationCache.MarkRevokedAsync(sessionId, _options.SessionAbsoluteTtl, cancellationToken);
        }

        private void EnsureAdmin(RoleLevel actorRole)
        {
            if (!_roleAuthorization.IsAdmin(actorRole))
                throw new AuthDomainException(AuthErrorCode.Forbidden, "Chỉ Admin mới được thực hiện thao tác này.");
        }

        private static SessionDto MapToDto(Session session, string? username) => new()
        {
            Id = session.Id,
            UserId = session.UserId,
            Username = username,
            DeviceInfo = session.DeviceInfo,
            IpAddress = session.IpAddress,
            LastActivityAt = session.LastActivityAt,
            ExpiresAt = session.ExpiresAt,
            IsRevoked = session.IsRevoked,
            CreatedDate = session.CreatedDate
        };
    }
}
