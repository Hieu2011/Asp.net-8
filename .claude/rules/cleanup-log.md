# Đã dọn dẹp (không cần đề xuất lại)

- Xóa `MongoLoggerService`/`IMongoLoggerService` tự viết — thay bằng `Serilog.Sinks.MongoDB`.
- Xóa `IMongoData`/`MongoData`/`IMongoDataFactory`/`MongoDataFactory` — dùng thẳng `IMongoDatabase`/`IMongoCollection<T>`, chỉ giữ 1 extension `GetPagedAsync<T>()` cho phần phân trang.
- Xóa code chết: `SerilogExtensions.cs`, method `SerilogConfig` trùng tên không ai gọi, `LogHelper.AddLog`/`enum LogType`.
- Secret thật trong `ApiCore8.Api/appsettings.json` (Postgres/Mongo/Redis password, Redis EncryptionKey) đã chuyển sang **User Secrets** — file JSON chỉ còn chuỗi rỗng, không commit giá trị thật nữa.
- Sửa 8 bug từ code review sau đợt dọn logging/Mongo: nối chuỗi connection string Mongo sai (dùng `MongoUrlBuilder`), so sánh giờ local với timestamp UTC (`DeleteOldLogsAsync`/`SearchAsync`), lọc Category bỏ sót log do thiếu `SourceContext` (thêm `Log.ForContext<T>()`), mất index/TTL của `SystemLogs` sau khi xóa `MongoLoggerService` (thêm `MongoIndexInitializer`, gọi 1 lần lúc app start), regex từ input người dùng không escape.
