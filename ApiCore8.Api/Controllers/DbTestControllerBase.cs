using ApiCore8.Api.Middleware;
using ApiCore8.Application.Abstractions;
using ApiCore8.Application.Contracts;
using ApiCore8.Application.Services;
using ApiCore8.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiCore8.Api.Controllers
{
    /// <summary>
    /// 5 action CRUD dùng chung cho cả 3 controller test DB (Postgres/Oracle/SqlServer) — mỗi
    /// controller cụ thể chỉ khác đúng 1 chỗ: cách dựng IDataCore (đọc connection string riêng
    /// của DB đó, không qua auto-detect DI — vì đã biết chắc đang test DB nào).
    /// Mỗi request tự tạo 1 IDataCore mới (không dùng chung/Scoped) rồi Dispose ngay sau khi xong —
    /// đơn giản, cô lập hoàn toàn giữa các request, không lo tranh chấp state.
    /// </summary>
    [LogApi]
    [ApiController]
    public abstract class DbTestControllerBase : ControllerBase
    {
        private readonly ILogger _logger;

        protected DbTestControllerBase(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>Dựng IDataCore riêng cho đúng DB đang test — implement ở từng controller con.</summary>
        protected abstract IDataCore CreateDataCore();

        [HttpPost("Users")]
        public async Task<APIResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
        {
            using var db = CreateDataCore();
            var repo = new UserRepository(db);
            try
            {
                var user = new Users
                {
                    Username = request.Username,
                    PasswordHash = request.PasswordHash,
                    FullName = request.FullName,
                    Email = request.Email,
                    IsActive = true
                };

                var created = await repo.CreateAsync(user, cancellationToken);
                return new APIResult(created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return new APIResult(true, ResultMessage.ErrorTypes.Insert, "Error creating user", ex.Message);
            }
        }

        [HttpGet("Users")]
        public async Task<APIResult> GetAllUsers(CancellationToken cancellationToken)
        {
            using var db = CreateDataCore();
            var repo = new UserRepository(db);
            try
            {
                var users = await repo.GetAllAsync(cancellationToken);
                return new APIResult(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users");
                return new APIResult(true, ResultMessage.ErrorTypes.GetData, "Error getting users", ex.Message);
            }
        }

        [HttpGet("Users/Fast")]
        public async Task<APIResult> GetAllUsersFast(CancellationToken cancellationToken)
        {
            // Y hệt GetAllUsers về kết quả trả về — khác cách đọc dữ liệu bên dưới (không qua
            // DataTable). So sánh tốc độ 2 cái qua header X-Response-Time-Ms (do [LogApi] gắn thêm).
            using var db = CreateDataCore();
            var repo = new UserRepository(db);
            try
            {
                var users = await repo.GetAllFastAsync(cancellationToken);
                return new APIResult(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users (fast)");
                return new APIResult(true, ResultMessage.ErrorTypes.GetData, "Error getting users", ex.Message);
            }
        }

        [HttpGet("Users/Search")]
        public async Task<APIResult> SearchByCreatedDate([FromQuery] string fromDate, [FromQuery] string toDate, CancellationToken cancellationToken)
        {
            // Nhận string thay vì để model binder tự parse thẳng DateTimeOffset — nếu thiếu offset,
            // model binder mặc định sẽ ÂM THẦM tự điền offset theo giờ server (không throw), gây sai
            // lệch giữa các môi trường. Tự validate bắt buộc phải có offset tường minh trước khi parse.
            if (!ExplicitOffsetDateTimeParser.TryParse(fromDate, out var fromDateOffset))
            {
                return new APIResult(true, ResultMessage.ErrorTypes.Validation,
                    "fromDate phải kèm offset múi giờ tường minh, VD: 2026-08-29T00:00:00+07:00 hoặc 2026-08-29T00:00:00Z", string.Empty);
            }

            if (!ExplicitOffsetDateTimeParser.TryParse(toDate, out var toDateOffset))
            {
                return new APIResult(true, ResultMessage.ErrorTypes.Validation,
                    "toDate phải kèm offset múi giờ tường minh, VD: 2026-08-29T23:59:59+07:00 hoặc 2026-08-29T23:59:59Z", string.Empty);
            }

            using var db = CreateDataCore();
            var repo = new UserRepository(db);
            try
            {
                var users = await repo.SearchByCreatedDateAsync(fromDateOffset.UtcDateTime, toDateOffset.UtcDateTime, cancellationToken);
                return new APIResult(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users by created date");
                return new APIResult(true, ResultMessage.ErrorTypes.SearchData, "Error searching users", ex.Message);
            }
        }

        [HttpGet("Users/{id:guid}")]
        public async Task<APIResult> GetUserById(Guid id, CancellationToken cancellationToken)
        {
            using var db = CreateDataCore();
            var repo = new UserRepository(db);
            try
            {
                var user = await repo.GetByIdAsync(id, cancellationToken);
                if (user == null)
                {
                    return new APIResult(true, ResultMessage.ErrorTypes.GetData, "User not found", string.Empty);
                }

                return new APIResult(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {UserId}", id);
                return new APIResult(true, ResultMessage.ErrorTypes.GetData, "Error getting user", ex.Message);
            }
        }

        [HttpPut("Users/{id:guid}")]
        public async Task<APIResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
        {
            using var db = CreateDataCore();
            var repo = new UserRepository(db);
            try
            {
                var user = new Users
                {
                    Id = id,
                    FullName = request.FullName,
                    Email = request.Email,
                    IsActive = request.IsActive
                };

                var success = await repo.UpdateAsync(user, cancellationToken);
                if (!success)
                {
                    return new APIResult(true, ResultMessage.ErrorTypes.Update, "User not found", string.Empty);
                }

                return new APIResult(new { Success = true, Message = "User updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                return new APIResult(true, ResultMessage.ErrorTypes.Update, "Error updating user", ex.Message);
            }
        }

        [HttpDelete("Users/{id:guid}")]
        public async Task<APIResult> DeleteUser(Guid id, CancellationToken cancellationToken)
        {
            using var db = CreateDataCore();
            var repo = new UserRepository(db);
            try
            {
                var success = await repo.DeleteAsync(id, cancellationToken);
                if (!success)
                {
                    return new APIResult(true, ResultMessage.ErrorTypes.Delete, "User not found", string.Empty);
                }

                return new APIResult(new { Success = true, Message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                return new APIResult(true, ResultMessage.ErrorTypes.Delete, "Error deleting user", ex.Message);
            }
        }
    }

    public class CreateUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class UpdateUserRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
