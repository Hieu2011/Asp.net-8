# Quy ước đặt tên

- **Không dùng tiền tố `BLL_`/`DAL_`/`IBLL_`** (tàn dư 3-layer cũ) — dùng PascalCase thuần: `IApiLogRepository`/`ApiLogRepository`, không phải `IBLL_ApiLogRepository`/`BLL_ApiLogRepository`.
- Không tự viết wrapper quanh thứ thư viện chính chủ đã có sẵn (bài học từ việc xóa `IMongoData`/`IMongoDataFactory` — dùng thẳng `IMongoDatabase`/`IMongoCollection<T>` của `MongoDB.Driver`).
- Ghi log: gọi `Log.Error(...)`/`Log.Information(...)`/`_logger.LogError(...)` **1 lần duy nhất** — Serilog tự fan-out ra Console/File/Graylog/Mongo theo cờ trong `appsettings.json` (`Serilog:EnableLogging:*`). Không viết pattern "dual logging" (gọi 2 hệ thống log thủ công) như code cũ.
- **Response API**: mọi action method trong controller PHẢI trả về `Task<APIResult>` (`ApiCore8.Application.Contracts.APIResult`) — kể cả khi service/repository bên dưới trả `PagedResult<T>` (search/pagination) thì controller vẫn phải bọc lại `new APIResult(pagedResult)` trước khi return, không bao giờ trả thẳng `PagedResult<T>`/DTO ra ngoài HTTP response. `PagedResult<T>` chỉ là DTO nội bộ giữa Application ↔ Api, không phải hợp đồng response cuối cùng với client.
