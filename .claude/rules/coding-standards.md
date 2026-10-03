# Coding standards (dùng chung ApiCore8 + AuthService + Shared)

Một kiểu code duy nhất cho toàn repo — không để src này một kiểu, src kia kiểu khác. Luôn chọn cách tối ưu hiệu năng nhất; thấy code cũ lệch chuẩn thì báo, không tự sửa lan ngoài phạm vi.

## 1. Tiền tố biến

Áp dụng cho **biến local, tham số method, private field** (field thêm `_` trước tiền tố: `_strConnectionString`). Viết tắt `ID` luôn hoa: `strShipmentOrderID`. Dùng `var` vẫn phải đặt tiền tố theo kiểu thực tế.

| Kiểu | Tiền tố | Ví dụ |
|---|---|---|
| `string` | `str` | `strShipmentOrderID` |
| `char` | `chr` | `chrSeparator` |
| `bool` | `bol` | `bolIsActive` |
| `byte` | `byt` | `bytFlag` |
| `short` | `sht` | `shtRoleLevel` |
| `int` | `int` | `intPageIndex` |
| `long` | `lng` | `lngTotalRecord` |
| `decimal` | `dec` | `decTotalAmount` |
| `double` / `float` | `dbl` / `flt` | `dblRate` |
| `DateTime` / `DateTimeOffset` | `dtm` | `dtmCreatedDate` |
| `TimeSpan` | `tsp` | `tspTimeout` |
| `Guid` | `guid` | `guidUserID` |
| `enum` | `enm` | `enmRoleLevel` |
| Class/DTO/entity | `obj` | `objUser` |
| `List`/`IEnumerable`/`ICollection` | `lst` | `lstUser` |
| Mảng `T[]` | `arr` | `arrByte` |
| `Dictionary` | `dic` | `dicHeader` |
| `HashSet` | `hst` | `hstRole` |
| `DataTable` / `DataSet` / `DataRow` | `dtb` / `dts` / `drw` | `dtbResult` |
| `StringBuilder` | `stb` | `stbSql` |
| `Task` chưa await | `tsk` | `tskLoadUser` |

**Ngoại lệ (không tiền tố):** public property/DTO/entity (PascalCase, là hợp đồng JSON/DB), hằng số `const` (PascalCase), field DI inject (`_userRepository`), `CancellationToken cancellationToken`, biến lambda ngắn (`x`), index vòng lặp (`i`, `j`).

## 2. Đặt tên khác

- Class/method/property: PascalCase. Interface: `I` + PascalCase. Không dùng tiền tố `BLL_`/`DAL_`/`IBLL_`.
- Method async luôn hậu tố `Async`; method trả `bool` bắt đầu bằng `Is`/`Has`/`Can`.
- Repository: `GetByIDAsync`, `GetListAsync`, `SearchAsync`, `InsertAsync`, `UpdateAsync`, `DeleteAsync`.
- **Store/function DB**: tên `sp_<bảng>_<hành động>` (snake_case), **tối đa 30 ký tự** (áp dụng store tạo mới; store cũ đã vượt thì giữ nguyên).
- **Biến trong store** (tham số + biến DECLARE): bắt đầu bằng `v_` + snake_case (`v_user_id`, `v_from_date`). Khớp `Shared.DataAccess`: C# truyền `AddParameter("@user_id", ...)` → helper tự đổi thành `v_user_id`; refcursor trả về luôn tên `v_out` (helper tự thêm, không khai ở C#).

## 3. Thứ tự member trong class

1. `const` → 2. `static readonly` → 3. field DI `private readonly` → 4. private field khác → 5. constructor → 6. public property → 7. public method → 8. protected method → 9. private method.

- 1 class/file (ngoại lệ: nhóm DTO request/response cùng nghiệp vụ trong `XxxContracts.cs`).
- File-scoped namespace. Class implementation mặc định `sealed` nếu không cần kế thừa.

## 4. Vị trí code (Clean Architecture — xem `solution-structure.md`)

| Layer | Thư mục | Chứa |
|---|---|---|
| Domain | gốc | Entity, enum — không phụ thuộc gì |
| Application | `Interfaces/`, `Services/`, `Contracts/`, `Options/` | Port, logic nghiệp vụ, DTO, options |
| Infrastructure | theo công nghệ: `Postgres/`, `Redis/`, `Security/`, `External/` | Implementation của port |
| Api | `Controllers/`, `Filters/`, `Middlewares/` | Controller mỏng: nhận request → gọi 1 service → `return new APIResult(...)` |
| Shared | `Shared/*` | Hàm dùng chung (DB, log, cache, convert, mã hóa) — có sẵn ở Shared thì KHÔNG viết lại trong service |

## 5. Response API

Mọi action trả `Task<APIResult>`. Kể cả service trả `PagedResult<T>`/DTO thì controller vẫn bọc `new APIResult(result)` — không trả thẳng DTO ra HTTP.

## 6. Async / đa luồng

- Mọi I/O dùng async, nhận `CancellationToken` ở tham số cuối và truyền xuống tận DB/Redis.
- **Đa luồng tối đa 5 luồng**: `Parallel.ForEachAsync` với `MaxDegreeOfParallelism = 5` hoặc `SemaphoreSlim(5)`. Không `Task.WhenAll` trên danh sách không giới hạn.

## 7. Logging

Gọi `Log.Error(...)`/`_logger.LogError(...)` **1 lần** — Serilog tự fan-out theo cờ `Serilog:EnableLogging:*`. Không dual logging thủ công.

## 8. Clean code & format

- Code gọn, đẹp, dễ đọc: không xuống hàng vô cớ, không dòng trống thừa (tối đa 1 dòng trống giữa các khối/method), không khoảng trắng thừa cuối dòng.
- Biểu thức ngắn viết 1 dòng (expression-bodied `=>`, khởi tạo object ngắn); chỉ xuống hàng khi dòng quá dài (~120 ký tự) hoặc để tách ý rõ hơn.
- Không để dead code, code comment-out, log debug, `using` thừa.

## 9. Comment

Tiếng Việt, ngắn gọn, dễ hiểu — 1 dòng là tốt nhất. Chỉ comment khi logic khó hiểu (nói *tại sao*, không kể lại code làm gì). XML doc cho public API 1-2 dòng. Không comment điều hiển nhiên.

## 10. Thư viện

- Không tự viết wrapper quanh thứ thư viện chính chủ đã có (dùng thẳng `IMongoDatabase`, `IConnectionMultiplexer`...).
- Version package thống nhất ở Shared; cài thêm package phải hỏi trước.
