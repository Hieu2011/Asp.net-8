using Shared.Common.Contracts;
using Shared.DataAccess.Abstractions;
using AuthService.Application.Contracts;
using AuthService.Application.Interfaces;
using AuthService.Domain;

namespace AuthService.Infrastructure.Postgres
{
    /// <inheritdoc cref="IUserRepository" />
    public class UserRepository : IUserRepository
    {
        private readonly IDataCore _db;

        public UserRepository(IDataCore db)
        {
            _db = db;
        }

        public async Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
        {
            var strUsername = string.IsNullOrEmpty(user.Username) ? null : user.Username;
            _db.AddParameter("@username", strUsername);
            _db.AddParameter("@email", user.Email);
            _db.AddParameter("@full_name", user.FullName);
            _db.AddParameter("@password_hash", user.PasswordHash);
            _db.AddParameter("@totp_secret", user.TotpSecret);
            _db.AddParameter("@role_level", user.RoleLevel);
            _db.AddParameter("@google_id", user.GoogleId);
            _db.AddParameter("@created_user", strUsername); // NULL → DB ghi chính username vừa sinh (mục 4)

            return await _db.ExecStoreObjectFastAsync<User>("sp_user_create", cancellationToken);
        }

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _db.AddParameter("@id", id);
            var user = await _db.ExecStoreObjectFastAsync<User>("sp_user_get_by_id", cancellationToken);
            return user.Id == Guid.Empty ? null : user;
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            _db.AddParameter("@email", email);
            var user = await _db.ExecStoreObjectFastAsync<User>("sp_user_get_by_email", cancellationToken);
            return user.Id == Guid.Empty ? null : user;
        }

        public async Task<User?> GetByGoogleIdAsync(string googleId, CancellationToken cancellationToken = default)
        {
            _db.AddParameter("@google_id", googleId);
            var user = await _db.ExecStoreObjectFastAsync<User>("sp_user_get_by_google_id", cancellationToken);
            return user.Id == Guid.Empty ? null : user;
        }

        public async Task<PagedResult<User>> GetPagedAsync(RoleLevel? roleLevel, string? keyword, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            _db.AddParameter("@role_level", roleLevel);
            _db.AddParameter("@keyword", string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim());
            _db.AddParameter("@page", page);
            _db.AddParameter("@page_size", pageSize);

            var lstRow = await _db.ExecStoreToListObjectAsync<UserPageRow>("sp_user_get_paged", cancellationToken);
            // total_count (COUNT(*) OVER()) giống nhau ở mọi dòng; trang rỗng thì tổng = 0
            var intTotal = lstRow.Count == 0 ? 0 : (int)lstRow[0].TotalCount;
            return new PagedResult<User> { Items = lstRow.Cast<User>().ToList(), Page = page, PageSize = pageSize, Total = intTotal };
        }

        public Task<UserStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
            => _db.ExecStoreObjectFastAsync<UserStatsDto>("sp_user_get_stats", cancellationToken);

        public async Task<bool> UpdatePasswordAsync(Guid id, string passwordHash, string updatedUser, CancellationToken cancellationToken = default)
        {
            _db.AddParameter("@id", id);
            _db.AddParameter("@password_hash", passwordHash);
            _db.AddParameter("@updated_user", updatedUser);

            var result = await _db.ExecuteNonQueryAsStringAsync("sp_user_update_password", cancellationToken);
            return ParseBoolResult(result);
        }

        public async Task<bool> UpdateTotpSecretAsync(Guid id, string? totpSecret, bool isTotpEnabled, string updatedUser, CancellationToken cancellationToken = default)
        {
            _db.AddParameter("@id", id);
            _db.AddParameter("@totp_secret", totpSecret);
            _db.AddParameter("@is_totp_enabled", isTotpEnabled);
            _db.AddParameter("@updated_user", updatedUser);

            var result = await _db.ExecuteNonQueryAsStringAsync("sp_user_update_totp_secret", cancellationToken);
            return ParseBoolResult(result);
        }

        public async Task<bool> SetFullNameAsync(Guid id, string fullName, string updatedUser, CancellationToken cancellationToken = default)
        {
            _db.AddParameter("@id", id);
            _db.AddParameter("@full_name", fullName);
            _db.AddParameter("@updated_user", updatedUser);

            var result = await _db.ExecuteNonQueryAsStringAsync("sp_user_set_full_name", cancellationToken);
            return ParseBoolResult(result);
        }

        public async Task<bool> LinkGoogleAsync(Guid id, string googleId, string updatedUser, CancellationToken cancellationToken = default)
        {
            _db.AddParameter("@id", id);
            _db.AddParameter("@google_id", googleId);
            _db.AddParameter("@updated_user", updatedUser);

            var result = await _db.ExecuteNonQueryAsStringAsync("sp_user_link_google", cancellationToken);
            return ParseBoolResult(result);
        }

        public async Task<bool> UpdateRoleAsync(Guid id, RoleLevel roleLevel, string updatedUser, CancellationToken cancellationToken = default)
        {
            _db.AddParameter("@id", id);
            _db.AddParameter("@role_level", roleLevel);
            _db.AddParameter("@updated_user", updatedUser);

            var result = await _db.ExecuteNonQueryAsStringAsync("sp_user_update_role", cancellationToken);
            return ParseBoolResult(result);
        }

        public async Task<bool> SetActiveAsync(Guid id, bool isActive, string updatedUser, CancellationToken cancellationToken = default)
        {
            _db.AddParameter("@id", id);
            _db.AddParameter("@is_active", isActive);
            _db.AddParameter("@updated_user", updatedUser);

            var result = await _db.ExecuteNonQueryAsStringAsync("sp_user_set_active", cancellationToken);
            return ParseBoolResult(result);
        }

        public async Task<bool> SetPermanentlyLockedAsync(Guid id, bool isPermanentlyLocked, string updatedUser, CancellationToken cancellationToken = default)
        {
            _db.AddParameter("@id", id);
            _db.AddParameter("@is_permanently_locked", isPermanentlyLocked);
            _db.AddParameter("@updated_user", updatedUser);

            var result = await _db.ExecuteNonQueryAsStringAsync("sp_user_set_permanently_locked", cancellationToken);
            return ParseBoolResult(result);
        }

        public async Task<bool> SetFailedOtpCountAsync(Guid id, int count, CancellationToken cancellationToken = default)
        {
            _db.AddParameter("@id", id);
            _db.AddParameter("@count", count);

            var result = await _db.ExecuteNonQueryAsStringAsync("sp_user_set_failed_otp_count", cancellationToken);
            return ParseBoolResult(result);
        }

        public async Task<bool> SoftDeleteAsync(Guid id, string deletedUser, CancellationToken cancellationToken = default)
        {
            _db.AddParameter("@id", id);
            _db.AddParameter("@deleted_user", deletedUser);

            var result = await _db.ExecuteNonQueryAsStringAsync("sp_user_soft_delete", cancellationToken);
            return ParseBoolResult(result);
        }

        /// <summary>Dòng của sp_user_get_paged — thêm cột total_count để phân trang.</summary>
        private sealed class UserPageRow : User
        {
            public long TotalCount { get; set; }
        }

        // Postgres trả "True"/"False" (boolean native) qua ExecuteScalar.ToString().
        private static bool ParseBoolResult(string result) => result.Trim() switch
        {
            "1" => true,
            "0" => false,
            _ => bool.TryParse(result, out var success) && success
        };
    }
}
