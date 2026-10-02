using Shared.DataAccess.Abstractions;
using AuthService.Application.Interfaces;

namespace AuthService.Infrastructure.Postgres
{
    /// <inheritdoc cref="ILoginHistoryRepository" />
    public class LoginHistoryRepository : ILoginHistoryRepository
    {
        private readonly IDataCore _db;

        public LoginHistoryRepository(IDataCore db)
        {
            _db = db;
        }

        public async Task InsertAsync(
            Guid? userId,
            string usernameAttempted,
            string? ipAddress,
            string? deviceInfo,
            string loginType,
            bool isSuccess,
            string? failureReason,
            CancellationToken cancellationToken = default)
        {
            _db.AddParameter("@user_id", userId);
            _db.AddParameter("@username_attempted", usernameAttempted);
            _db.AddParameter("@ip_address", ipAddress);
            _db.AddParameter("@device_info", deviceInfo);
            _db.AddParameter("@login_type", loginType);
            _db.AddParameter("@is_success", isSuccess);
            _db.AddParameter("@failure_reason", failureReason);

            // sp_login_history_insert trả về uuid nhưng caller không cần dùng — chỉ audit, không
            // qua refcursor (không phải sp_*_get_*), nên gọi thẳng ExecuteNonQueryAsync.
            await _db.ExecuteNonQueryAsync("sp_login_history_insert", cancellationToken);
        }
    }
}
