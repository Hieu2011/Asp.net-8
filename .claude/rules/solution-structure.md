# Cấu trúc solution

```
/
├─ src/
│  ├─ ApiCore8/      ApiCore8.sln — Business API: ApiCore8.{Api,Application,Infrastructure,Domain}, tests/
│  ├─ AuthService/   AuthService.sln — Auth/SSO: AuthService.{Api,Application,Infrastructure,Domain}
│  └─ Shared/        Shared.sln — thư viện dùng chung: Shared.*
├─ frontend/         Next.js 16 (pnpm) — web bán hàng, gọi AuthService qua BFF; rule: frontend-standards.md
├─ Dockerfile.apicore8, Dockerfile.authservice   (ở root — convention Coolify, context = root)
├─ Directory.Build.props      net8.0/Nullable/ImplicitUsings chung — csproj KHÔNG khai lại
├─ Directory.Packages.props   Central Package Management — version chỉ khai ở đây, csproj ghi <PackageReference Include="X" />
└─ All.sln           gộp mọi project để mở/debug trong VS — không dùng cho CI
```

- **Ranh giới:** ApiCore8 và AuthService không `ProjectReference` chéo nhau (giao tiếp qua JWT/API). Cả 2 chỉ reference `Shared.*`; Shared không reference service nào. Mỗi sln service add project Shared vào solution folder `Shared`.
- **Clean Architecture** (mỗi service): `Api → Infrastructure → Application → Domain`, một chiều, `Domain` không phụ thuộc gì. Interface ở `Application`, implementation ở `Infrastructure`, DI qua `AddApplicationServices()`/`AddInfrastructureServices()`, gọi trong `Api/StartupConfig.cs`.
- **Shared:** code dùng chung (DB, log, cache, convert, mã hóa, response envelope) chỉ đặt ở Shared — không viết lại trong service.
  - `Shared.Common`: `APIResult`/`PagedResult`/`ResultMessage`, `Helpers/` (convert, string, object, Excel), `Security/AesGzipCipher`, `Constants/ThreadingConstants`
  - `Shared.DataAccess`: `IDataCore`, mapper, Postgres/Oracle/SqlServer helper, `DataCoreFactory`, `Mongo/` (`MongoDatabaseFactory`, `GetPagedAsync`)
  - `Shared.Caching`: `Redis/` (`RedisConnectionFactory`, `IRedisConnectionService`/`RedisConnectionService`)
  - `Shared.Common/Health`: `HealthCheckProbe` (`/health` + chế độ `--healthcheck` cho Coolify)
  - `Shared.Common/Exceptions`: `ExceptionDataExtensions` (`SetStoreName`/`GetStoreName` — DbHelper gắn tên store vào exception thay vì tự log; middleware + `[LogApi]` ghi field `StoreName`)
  - `Shared.Common/Middlewares`: `GlobalExceptionMiddleware` (`app.UseGlobalExceptionHandler()` đầu pipeline — exception chưa bắt → log + APIResult 500 kèm traceId, không lộ stack trace)
  - `Shared.Logging`: `LoggingStartupConfig` (Serilog Console/File/Graylog/Mongo), `MongoLogIndexInitializer` (index + TTL log), `ApiLog/` (`[LogApi]` → hàng đợi → Mongo, ghi StatusCode thật sau response + IsError/TraceID/ErrorType/ErrorMessage/StackTrace (5xx), `ApiLogSanitizer` che field nhạy cảm + cắt chuỗi dài)
  - Trong Shared: `DataAccess`, `Caching`, `Logging` → `Common` (Common không phụ thuộc project Shared nào).
  - Reference: `*.Application` → Common + DataAccess (+ Logging ở ApiCore8 vì dùng entity `ApiExecutionLog`); `*.Infrastructure` → Caching (+ Logging ở ApiCore8); `AuthService.Api` → Logging.
- Thêm project mới: cập nhật sln của service + `All.sln` + Dockerfile (COPY csproj trước restore).
- **Script DB** (tạo bảng, store, seed): KHÔNG để trong `src/` — lưu ở `TaskList/<Src>/_scripts/` (local, không commit).
