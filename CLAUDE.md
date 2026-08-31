# WebApiCore8 / AuthService — Project Notes

Ghi lại các quyết định kiến trúc, roadmap và quy ước đã thống nhất qua nhiều phiên làm việc — đọc trước khi đề xuất thay đổi lớn, tránh lặp lại các câu hỏi/quyết định đã chốt.

## Cấu trúc solution hiện tại

Monorepo, 2 solution riêng biệt, **không có `ProjectReference` chéo giữa 2 solution** (ranh giới microservice — mỗi bên chỉ giao tiếp qua JWT/API sau này):

```
D:\Asp.net-source\Asp.net-8\
  WebApiCore8.sln              — Business API (đã có, đang chạy)
    ApiCore8.Domain/
    ApiCore8.Application/
    ApiCore8.Infrastructure/
    ApiCore8.Api/
  AuthService/
    AuthService.sln            — Auth/Identity service (mới, đang là skeleton)
    AuthService.Domain/
    AuthService.Application/
    AuthService.Infrastructure/
    AuthService.Api/
```

Cả 2 đều theo Clean Architecture: `Api → Infrastructure → Application → Domain`, một chiều duy nhất, `Domain` không phụ thuộc gì. Interface/port khai báo ở `Application`, implementation ở `Infrastructure`, đăng ký DI qua extension `AddApplicationServices()`/`AddInfrastructureServices()`, gọi gộp trong `Api/StartupConfig.cs`.

## Quy ước đặt tên

- **Không dùng tiền tố `BLL_`/`DAL_`/`IBLL_`** (tàn dư 3-layer cũ) — dùng PascalCase thuần: `IApiLogRepository`/`ApiLogRepository`, không phải `IBLL_ApiLogRepository`/`BLL_ApiLogRepository`.
- Không tự viết wrapper quanh thứ thư viện chính chủ đã có sẵn (bài học từ việc xóa `IMongoData`/`IMongoDataFactory` — dùng thẳng `IMongoDatabase`/`IMongoCollection<T>` của `MongoDB.Driver`).
- Ghi log: gọi `Log.Error(...)`/`Log.Information(...)`/`_logger.LogError(...)` **1 lần duy nhất** — Serilog tự fan-out ra Console/File/Graylog/Mongo theo cờ trong `appsettings.json` (`Serilog:EnableLogging:*`). Không viết pattern "dual logging" (gọi 2 hệ thống log thủ công) như code cũ.

## Roadmap Auth/SSO (đang ở đầu Giai đoạn 4)

1. **Hạ tầng & CI/CD** — xong phần lớn: Coolify quản container trên VM (laptop cá nhân), Cloudflare Tunnel public app khi ở ngoài mạng công ty, Tailscale cho anh tự vào Coolify Dashboard khi ở công ty (Cloudflare bị chặn). CI/CD: **Coolify webhook tự động khi ở nhà, tự bấm Redeploy qua Tailscale khi ở công ty** — không dùng self-hosted GitHub Actions runner (rủi ro supply-chain trên VM dùng chung nhiều service) và không dùng ngrok (URL đổi liên tục, rủi ro chính sách công ty).
2. **Database** — Postgres mới tạo trên VM, **dùng chung 1 instance nhưng tách riêng database** (`auth_db` cho Auth, database khác cho Business) để giữ ranh giới. Redis riêng cho cache/rate-limit/đếm OTP sai. MongoDB tái dùng cho log (đã có sẵn).
3. **Auth Service** (đang code):
   - Đăng ký: hash password, sinh QR code TOTP (Google Authenticator).
   - Đăng nhập: **Username + OTP là chính**, sai OTP **3 lần liên tiếp** mới fallback bắt nhập Password.
   - Issue JWT (RSA, tự ký — **không dùng Keycloak/OpenIddict**, đã cân nhắc kỹ: Keycloak tốn thêm RAM đáng kể trên VM laptop vốn đã chật, và luồng OTP-trước-Password-sau không khớp mặc định của Keycloak).
   - Quản lý session: lưu thiết bị/IP/thời gian đăng nhập, cho phép liệt kê + thu hồi session hoặc vô hiệu hóa cả user.
4. **Business API** — sau khi Auth xong: thêm JWT Bearer validation bằng RSA public key của Auth Service, gắn `[Authorize]`, bật rate limit built-in .NET 8 (thay `AntiSpamMiddleware` đang tắt).

## Đã dọn dẹp (không cần đề xuất lại)

- Xóa `MongoLoggerService`/`IMongoLoggerService` tự viết — thay bằng `Serilog.Sinks.MongoDB`.
- Xóa `IMongoData`/`MongoData`/`IMongoDataFactory`/`MongoDataFactory` — dùng thẳng `IMongoDatabase`/`IMongoCollection<T>`, chỉ giữ 1 extension `GetPagedAsync<T>()` cho phần phân trang.
- Xóa code chết: `SerilogExtensions.cs`, method `SerilogConfig` trùng tên không ai gọi, `LogHelper.AddLog`/`enum LogType`.
- Secret thật trong `ApiCore8.Api/appsettings.json` (Postgres/Mongo/Redis password, Redis EncryptionKey) đã chuyển sang **User Secrets** — file JSON chỉ còn chuỗi rỗng, không commit giá trị thật nữa.
- Sửa 8 bug từ code review sau đợt dọn logging/Mongo: nối chuỗi connection string Mongo sai (dùng `MongoUrlBuilder`), so sánh giờ local với timestamp UTC (`DeleteOldLogsAsync`/`SearchAsync`), lọc Category bỏ sót log do thiếu `SourceContext` (thêm `Log.ForContext<T>()`), mất index/TTL của `SystemLogs` sau khi xóa `MongoLoggerService` (thêm `MongoIndexInitializer`, gọi 1 lần lúc app start), regex từ input người dùng không escape.

## Chưa làm / vấn đề đã biết (không phải bug mới)

- `AntiSpamMiddleware` viết xong nhưng đang tắt trong `Program.cs`.
- `GlobalExceptionMiddleware` trả stack trace về client — chưa mask.
- `ApiLoggingAttribute`/`LogApiAttribute` log request/response body chưa mask field nhạy cảm (password...).
- Chưa có `RequestTimeouts` middleware (.NET 8) cho request chạy lâu; các method repository (`ApiLogRepository`, `SystemLogRepository`, `RedisCacheRepository`) chưa nhận `CancellationToken` nên timeout ở tầng HTTP sẽ không cắt được câu query DB đang chạy ngầm.
- `.gitignore` chưa loại trừ `ApiCore8.Api/logs/` — log file (rolling theo ngày, giữ 7 file) từng bị commit nhầm vào git trước đây.
- Đã đánh giá `ai-memory` (github.com/akitaonrails/ai-memory, MCP memory server đa-tool) — quyết định **không cài** vì trùng chức năng với memory sẵn có của Claude Code, trừ khi sau này dùng thêm AI coding tool khác ngoài Claude Code trên cùng project.

## Đang làm gì / bước hiện tại

Xem **`PROJECT_STATUS.md`** (cùng thư mục gốc) — đọc file đó TRƯỚC khi trả lời bất kỳ câu hỏi nào về tiến độ/việc đang dở, không hỏi lại bối cảnh từ đầu. File đó cập nhật thường xuyên hơn file này nên là nguồn đúng nhất cho "đang ở bước nào".

## Cách làm việc đã thống nhất

- Thay đổi lớn/nhiều file: trình roadmap/plan trước, đợi duyệt rồi mới code.
- **Chỉ sửa file, không tự ý `git commit`** — sửa/fix xong cứ để ở working tree, đợi anh yêu cầu rõ ràng mới commit.
- Trước `git push`: hỏi xác nhận.
- Giải thích khái niệm .NET Core mới (DI, async, tuple...) từ bản chất kèm ví dụ — anh đang chuyển từ .NET Framework.
