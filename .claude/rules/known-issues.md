# Chưa làm / vấn đề đã biết (không phải bug mới)

- `AntiSpamMiddleware` viết xong nhưng đang tắt trong `Program.cs`.
- `GlobalExceptionMiddleware` trả stack trace về client — chưa mask.
- `ApiLoggingAttribute`/`LogApiAttribute` log request/response body chưa mask field nhạy cảm (password...).
- Chưa có `RequestTimeouts` middleware (.NET 8) cho request chạy lâu; các method repository (`ApiLogRepository`, `SystemLogRepository`, `RedisCacheRepository`) chưa nhận `CancellationToken` nên timeout ở tầng HTTP sẽ không cắt được câu query DB đang chạy ngầm.
- `.gitignore` chưa loại trừ `ApiCore8.Api/logs/` — log file (rolling theo ngày, giữ 7 file) từng bị commit nhầm vào git trước đây.
- Đã đánh giá `ai-memory` (github.com/akitaonrails/ai-memory, MCP memory server đa-tool) — quyết định **không cài** vì trùng chức năng với memory sẵn có của Claude Code, trừ khi sau này dùng thêm AI coding tool khác ngoài Claude Code trên cùng project.
