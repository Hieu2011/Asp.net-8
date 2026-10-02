using Shared.Common.Contracts;
using AuthService.Application.Contracts;
using AuthService.Application.Interfaces;
using AuthService.Application.Options;
using AuthService.Domain;
using Microsoft.Extensions.Options;

namespace AuthService.Application.Services
{
    /// <inheritdoc cref="IUserManagementService" />
    public class UserManagementService : IUserManagementService
    {
        private readonly IUserRepository _userRepository;
        private readonly ISessionRepository _sessionRepository;
        private readonly ISessionRevocationCache _revocationCache;
        private readonly IOtpLockoutService _otpLockout;
        private readonly IRoleAuthorizationService _roleAuthorization;
        private readonly IUserProfileCache _profileCache;
        private readonly AuthOptions _options;

        public UserManagementService(
            IUserRepository userRepository,
            ISessionRepository sessionRepository,
            ISessionRevocationCache revocationCache,
            IOtpLockoutService otpLockout,
            IRoleAuthorizationService roleAuthorization,
            IUserProfileCache profileCache,
            IOptions<AuthOptions> options)
        {
            _userRepository = userRepository;
            _sessionRepository = sessionRepository;
            _revocationCache = revocationCache;
            _otpLockout = otpLockout;
            _roleAuthorization = roleAuthorization;
            _profileCache = profileCache;
            _options = options.Value;
        }

        public async Task<PagedResult<UserDto>> GetUsersAsync(RoleLevel? roleLevel, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var paged = await _userRepository.GetPagedAsync(roleLevel, page, pageSize, cancellationToken);
            return new PagedResult<UserDto>
            {
                Items = paged.Items.Select(MapToDto).ToList(),
                Total = paged.Total,
                Page = paged.Page,
                PageSize = paged.PageSize
            };
        }

        public async Task<UserDto> GetMyProfileAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var cached = await _profileCache.GetAsync(userId, cancellationToken);
            if (cached != null)
                return cached;

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
                ?? throw new AuthDomainException(AuthErrorCode.UserNotFound, "Không tìm thấy tài khoản.");

            var dto = MapToDto(user);
            await _profileCache.SetAsync(userId, dto, cancellationToken);
            return dto;
        }

        public async Task ChangeRoleAsync(Guid actorId, RoleLevel actorRole, string actorUsername, Guid targetId, RoleLevel newRoleLevel, CancellationToken cancellationToken = default)
        {
            EnsureAdminAndNotSelf(actorId, actorRole, targetId);

            await _userRepository.UpdateRoleAsync(targetId, newRoleLevel, actorUsername, cancellationToken);
            await _profileCache.RemoveAsync(targetId, cancellationToken);

            // Mục 34 — JWT cũ có thể còn mang role_level cũ tối đa 5 phút, revoke session ép logout ngay.
            await SessionRevocationHelper.RevokeAllAndMarkCacheAsync(
                _sessionRepository, _revocationCache, targetId, actorUsername, _options.SessionAbsoluteTtl, cancellationToken);
        }

        public async Task DisableAsync(Guid actorId, RoleLevel actorRole, string actorUsername, Guid targetId, CancellationToken cancellationToken = default)
        {
            EnsureAdminAndNotSelf(actorId, actorRole, targetId);

            await _userRepository.SetActiveAsync(targetId, false, actorUsername, cancellationToken);
            await _profileCache.RemoveAsync(targetId, cancellationToken);
            await SessionRevocationHelper.RevokeAllAndMarkCacheAsync(
                _sessionRepository, _revocationCache, targetId, actorUsername, _options.SessionAbsoluteTtl, cancellationToken);
        }

        public async Task DeleteAsync(Guid actorId, RoleLevel actorRole, string actorUsername, Guid targetId, CancellationToken cancellationToken = default)
        {
            EnsureAdminAndNotSelf(actorId, actorRole, targetId);

            await _userRepository.SoftDeleteAsync(targetId, actorUsername, cancellationToken);
            await _profileCache.RemoveAsync(targetId, cancellationToken);
            await SessionRevocationHelper.RevokeAllAndMarkCacheAsync(
                _sessionRepository, _revocationCache, targetId, actorUsername, _options.SessionAbsoluteTtl, cancellationToken);
        }

        public async Task UnlockAsync(string actorUsername, Guid targetId, CancellationToken cancellationToken = default)
        {
            await _otpLockout.ClearAllLocksAsync(targetId, cancellationToken);
            await _userRepository.SetPermanentlyLockedAsync(targetId, false, actorUsername, cancellationToken);
            await _profileCache.RemoveAsync(targetId, cancellationToken);
        }

        private void EnsureAdminAndNotSelf(Guid actorId, RoleLevel actorRole, Guid targetId)
        {
            // Attribute [RequireMinRoleLevel(Admin)] ở Controller đã chặn phần lớn, nhưng Service tự
            // check lại (defense-in-depth, mục 14) — không phụ thuộc hoàn toàn vào 1 lớp bảo vệ duy nhất.
            if (!_roleAuthorization.IsAdmin(actorRole))
                throw new AuthDomainException(AuthErrorCode.Forbidden, "Chỉ Admin mới được thực hiện thao tác này.");

            if (_roleAuthorization.IsSelfTarget(actorId, targetId))
                throw new AuthDomainException(AuthErrorCode.SelfTargetNotAllowed, "Không thể thao tác lên chính tài khoản của mình.");
        }

        private static UserDto MapToDto(User user) => new()
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            RoleLevel = user.RoleLevel,
            IsActive = user.IsActive,
            IsPermanentlyLocked = user.IsPermanentlyLocked,
            IsTotpEnabled = user.IsTotpEnabled,
            HasGoogleLinked = !string.IsNullOrEmpty(user.GoogleId),
            CreatedDate = user.CreatedDate
        };
    }
}
