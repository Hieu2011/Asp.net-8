using ApiCore8.Application.Abstractions;
using ApiCore8.Application.Interfaces;
using ApiCore8.Domain.Entities;

namespace ApiCore8.Application.Services
{
    /// <summary>
    /// Repository test cho Postgres — gọi qua Postgres function (SP), không viết SQL thô trong code.
    /// App DB user chỉ cần quyền EXECUTE trên các function này, không cần quyền SELECT/INSERT/UPDATE/DELETE
    /// trực tiếp trên bảng "users" — giới hạn rủi ro dù code có bug/bị chèn gì đi nữa.
    /// Xem SQL tạo bảng + function + GRANT/REVOKE trong PROJECT_STATUS.md.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly IDataCore _db;

        public UserRepository(IDataCore db)
        {
            _db = db;
        }

        public async Task<Users> CreateAsync(Users user, CancellationToken cancellationToken = default)
        {
            _db.AddParameter("p_username", user.Username);
            _db.AddParameter("p_password_hash", user.PasswordHash);
            _db.AddParameter("p_full_name", user.FullName);
            _db.AddParameter("p_email", user.Email);

            return await _db.ExecStoreToObjectAsync<Users>("sp_user_create", cancellationToken);
        }

        public async Task<Users?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _db.AddParameter("p_id", id);

            var user = await _db.ExecStoreToObjectAsync<Users>("sp_user_get_by_id", cancellationToken);
            return user?.Id == Guid.Empty ? null : user;
        }

        public async Task<List<Users>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _db.ExecStoreToListObjectAsync<Users>("sp_user_get_all", cancellationToken);
        }

        public async Task<List<Users>> SearchByCreatedDateAsync(DateTime fromDateUtc, DateTime toDateUtc, CancellationToken cancellationToken = default)
        {
            _db.AddParameter("p_from_date", fromDateUtc);
            _db.AddParameter("p_to_date", toDateUtc);

            return await _db.ExecStoreToListObjectAsync<Users>("sp_user_search_by_date", cancellationToken);
        }

        public async Task<bool> UpdateAsync(Users user, CancellationToken cancellationToken = default)
        {
            _db.AddParameter("p_id", user.Id);
            _db.AddParameter("p_full_name", user.FullName);
            _db.AddParameter("p_email", user.Email);
            _db.AddParameter("p_is_active", user.IsActive);

            var result = await _db.ExecuteNonQueryAsStringAsync("sp_user_update", cancellationToken);
            return ParseBoolResult(result);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _db.AddParameter("p_id", id);

            var result = await _db.ExecuteNonQueryAsStringAsync("sp_user_delete", cancellationToken);
            return ParseBoolResult(result);
        }

        // Postgres trả "True"/"False" (kiểu boolean native). Oracle/SQL Server trả "1"/"0"
        // (không lộ boolean PL/SQL ra ngoài .NET được, dùng NUMBER/BIT thay thế) — bool.TryParse
        // không tự hiểu "1"/"0", nên phải tự xử lý cả 2 dạng ở đây.
        private static bool ParseBoolResult(string result) => result.Trim() switch
        {
            "1" => true,
            "0" => false,
            _ => bool.TryParse(result, out var success) && success
        };
    }
}
