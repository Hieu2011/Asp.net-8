# Cấu trúc solution

```
/
├─ src/
│  ├─ ApiCore8/      ApiCore8.sln — Business API: ApiCore8.{Api,Application,Infrastructure,Domain}, tests/
│  ├─ AuthService/   AuthService.sln — Auth/SSO: AuthService.{Api,Application,Infrastructure,Domain}
│  └─ Shared/        Shared.sln — thư viện dùng chung: Shared.*
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
  - `Shared.Logging`: `LoggingStartupConfig` (Serilog Console/File/Graylog/Mongo)
  - Reference: `*.Application` → Common + DataAccess; `*.Infrastructure` → Caching (+ Logging ở ApiCore8).
- Thêm project mới: cập nhật sln của service + `All.sln` + Dockerfile (COPY csproj trước restore).
- **Script DB** (tạo bảng, store, seed): KHÔNG để trong `src/` — lưu ở `TaskList/<Src>/_scripts/` (local, không commit).
