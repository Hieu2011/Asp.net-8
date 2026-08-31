# Bản đồ tiến độ — đọc file này đầu tiên khi bắt đầu session mới

Quy ước: mỗi khi 1 việc xong → xóa khỏi "Đang làm", chuyển ghi chú quan trọng (nếu có) vào `CLAUDE.md`. Việc đã commit → xóa khỏi file này hoàn toàn, không giữ làm lịch sử (git log lo phần đó). File này chỉ phản ánh **hiện tại đang ở đâu**, không phải nhật ký.

## Bản đồ giai đoạn lớn (macro roadmap)

- [x] **Giai đoạn 1 — Hạ tầng & CI/CD**: Coolify + Cloudflare Tunnel + Tailscale, xong phần lớn.
- [x] **Giai đoạn 2 — Database**: Postgres (`auth_db` riêng), Redis riêng, MongoDB tái dùng cho log.
- [~] **Giai đoạn 3 — Dọn dẹp WebApiCore8 (Business API)**: Clean Architecture xong, logging/Mongo pipeline xong (vừa fix xong đợt bug lớn — xem "Đang làm" bên dưới). Còn sót: AntiSpamMiddleware tắt, mask log nhạy cảm, RequestTimeouts/CancellationToken.
- [ ] **Giai đoạn 4 — Auth Service**: mới ở mức skeleton project, CHƯA code business logic (Users/OTP/JWT/Session). Chưa bắt đầu.

→ **Đang ở cuối Giai đoạn 3, chuẩn bị bắt đầu Giai đoạn 4.**

## Đang làm (chi tiết, session gần nhất — 2026-08-23)

Chủ đề: siết timeout connection string, truy gốc bug "log không ghi vào Mongo", làm lại thư viện Postgres.

**Đã xong, ở working tree, CHƯA commit:**
- Timeout: Postgres (`Timeout=5;Command Timeout=10`), Mongo (`serverSelectionTimeoutMS=5000&connectTimeoutMS=5000`) — qua User Secrets.
- `RedisTestController.SetCache`: fix `expiryMinutes: 0` → lỗi Redis `invalid expire time`; fix `expiry.Value` crash theo sau.
- Gắn `[LogApi]` vào `RedisTestController`.
- Root cause chuỗi bug Mongo logging (4 lớp, xem chi tiết trong git diff khi cần):
  1. `LoggingStartupConfig.AddSerilog` — `MongoUrlBuilder` đổi `DatabaseName` làm lệch `authSource` ngầm định → `MongoAuthenticationException`. Fix: chốt `AuthenticationSource` trước.
  2. `SystemLog` thiếu `[BsonIgnoreExtraElements]` → `FormatException` khi đọc field lạ (`MessageTemplate`).
  3. `SystemLog.Exception` sai kiểu `string?` → đổi `BsonDocument?`.
  4. `System.Text.Json` không serialize được `BsonDocument` → thêm `BsonDocumentJsonConverter` (`ApiCore8.Domain/Serialization/`).
- Verify bằng `dotnet run` thật + `curl` — `SystemLogsController.Recent` trả đúng log thật.
- Thêm `Serilog.Debugging.SelfLog.Enable(...)` (giữ lại vĩnh viễn, không phải tạm).
- `ApiExecutionLog.CreatedAt` + `ApiLoggingAttribute` (`_startTime`/`endTime`/`CreatedAt`): đổi `DateTime.Now` → `DateTime.UtcNow` — đồng bộ UTC với `SystemLog` (trước lệch múi giờ giữa 2 collection log).
- **Viết lại thư viện Postgres theo hướng gọi Postgres function/SP (không raw SQL)** — anh muốn giới hạn quyền DB user (chỉ `GRANT EXECUTE` trên function, `REVOKE` quyền trực tiếp trên bảng):
  - `PostgresDbHelper.cs`: fix 3 bug (thiếu `@` ở `ExecuteNonQueryAsync`/`ExecuteNonQueryAsStringAsync`, `Convert.ChangeType` crash với `Guid`, gọi theo *named notation* `tên => @tên` thay vì positional để thứ tự tham số trong function không cần khớp thứ tự khai báo); bỏ `ConfigHelper` (tự đọc `appsettings.json` riêng, không thấy User Secrets); thêm transaction cục bộ tự mở/đóng quanh refcursor (OPEN/FETCH/CLOSE) vì cursor chỉ sống trong 1 transaction.
  - `ConfigHelper.cs` vẫn giữ nguyên file (không xóa) nhưng không dùng nữa — có bug riêng (không đọc User Secrets) nếu sau này định dùng lại.
  - `IDataCore` đăng ký DI kiểu Scoped (`AddInfrastructureServices`), lấy connection string qua `IConfiguration`.
  - `IUserRepository`/`UserRepository` (Application) + `PostgresTestController` (`/api/PostgresTest/Users`) — CRUD test gọi qua 5 function: `sp_user_create`, `sp_user_get_by_id`, `sp_user_get_all`, `sp_user_update`, `sp_user_delete`.
  - Build `WebApiCore8.sln`: 0 lỗi.
  - **Anh cần tự chạy SQL tạo 5 function + GRANT/REVOKE trong DB** (đã đưa trong chat, bảng `users` anh đã tạo sẵn) — chưa verify chạy thật qua Swagger/curl.

**Ghi chú tham khảo — pattern code cũ `tmsinternalapi` (`D:\Project_TGDD\InternalAPI_Version2\tmsinternalapi`), dùng để cải thiện `PostgresDbHelper`/`IDataCore` sau này:**
- BLL tạo `IData objData = Data.CreateData();` 1 lần đầu method, **tự tay truyền `objData` làm tham số** qua mọi hàm DAL cần dùng trong cùng 1 nghiệp vụ (không qua DI).
- Mở/đóng tách bước rõ ràng: `objData.Connect()` / `objData.BeginTransaction()` / `objData.CommitTransaction()` / `objData.Disconnect()` — gọi `Disconnect()` lại lần nữa trong `finally` (idempotent) để đảm bảo đóng dù exception xảy ra ở đâu. Không dùng `using(){}`.
- DAL gọi function Postgres qua `objData.CreateNewStoredProcedure("schema.function_name")` + `AddParameter("v_xxx", value)` + `ExecStoreToDataTable()`/`ExecNonQuery()` — tên tham số Postgres theo quy ước `v_xxx`, không prefix `@`.
- So với `PostgresDbHelper` mới: cùng ý tưởng (1 connection dùng chung cho 1 đơn vị công việc, không mở/đóng mỗi câu SQL), khác cơ chế (DI tự inject + tự Dispose khi hết Scope, thay vì tự tay truyền tham số + tự viết `finally { Disconnect() }`). Việc cần làm sau: cân nhắc học theo pattern `CreateNewStoredProcedure(name)` tách riêng khỏi `AddParameter` (rõ ràng hơn cách hiện tại truyền `storeName` trực tiếp vào `ExecStoreToObjectAsync<T>(storeName)`), và cân nhắc có cần API `Connect()`/`Disconnect()` tường minh cho trường hợp 1 nghiệp vụ cần nhiều bước transaction lồng nhau.

**Việc kế tiếp, chưa làm — pending quyết định của anh:**
1. Chạy SQL tạo 5 function `sp_user_*` + GRANT/REVOKE vào DB, verify `/api/PostgresTest/Users` chạy thật.
2. `git commit` đợt fix trên (đang chờ anh confirm).
3. Thêm `ApiCore8.Api/logs/` vào `.gitignore`.
4. `RequestTimeouts` middleware (.NET 8) + truyền `CancellationToken` xuống `ApiLogRepository`/`SystemLogRepository`/`RedisCacheRepository`.
5. Sau đó: bắt đầu Giai đoạn 4 (Auth Service — Users/OTP/JWT/Session) nếu Giai đoạn 3 coi như đóng.

## Cách dùng file này ở session mới

1. Đọc file này trước, không hỏi lại bối cảnh từ đầu.
2. Báo ngắn gọn: "đang ở [mục X trong Đang làm / Việc kế tiếp]" rồi hỏi đúng 1 câu để biết làm tiếp mục nào.
3. Việc nào xong trong session → cập nhật lại đúng file này ngay (không đợi nhắc).
