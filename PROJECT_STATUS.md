# Bản đồ tiến độ — đọc file này đầu tiên khi bắt đầu session mới

Quy ước: mỗi khi 1 việc xong → xóa khỏi "Đang làm", chuyển ghi chú quan trọng (nếu có) vào `CLAUDE.md`. Việc đã commit → xóa khỏi file này hoàn toàn, không giữ làm lịch sử (git log lo phần đó). File này chỉ phản ánh **hiện tại đang ở đâu**, không phải nhật ký.

## Bản đồ giai đoạn lớn (macro roadmap — 5 bước)

- [~] **Bước 1 — Hạ tầng & CI/CD**: Coolify + Cloudflare Tunnel + Tailscale — đã cài đặt, chạy thật. Riêng **mô hình CI/CD (Coolify webhook tự động ở nhà, redeploy tay qua Tailscale ở công ty) mới CHỐT quyết định, CHƯA triển khai/test thật** — không tính là xong hẳn.
- [x] **Bước 2 — Database**: Postgres (`auth_db` riêng), Redis riêng, MongoDB tái dùng cho log.
- [~] **Bước 3 — Dọn dẹp & tối ưu WebApiCore8**: Clean Architecture xong, multi-provider data access (Postgres/Oracle/SqlServer) xong, pipeline đọc dữ liệu tối ưu (CompiledReaderMapper) xong, logging Mongo + API search xong. Còn sót: AntiSpamMiddleware tắt, mask log nhạy cảm, RequestTimeouts/CancellationToken, Oracle/SqlServer chưa verify chạy thật. **Đây là dọn dẹp/chuẩn bị nền — KHÁC bước 5 (gắn JWT thật).**
- [ ] **Bước 4 — Auth Service**: mới ở mức skeleton project, CHƯA code business logic (Users/OTP/JWT/Session). Chưa bắt đầu.
- [ ] **Bước 5 — Business API: tích hợp JWT**: sau khi bước 4 xong — thêm JWT Bearer validation (RSA public key của Auth Service), gắn `[Authorize]`, bật rate limit built-in .NET 8 (thay `AntiSpamMiddleware`). Phụ thuộc thẳng bước 4, chưa bắt đầu.

→ **Đang ở bước 3 (dọn dẹp/tối ưu WebApiCore8), bước 1 còn 1 hạng mục CI/CD chưa triển khai thật.**

## Trạng thái git

Toàn bộ việc bên dưới **đã nằm trong commit `87ba61e` ("upcode init project")** — working tree hiện đang sạch (`git status` clean). Chưa push lên remote nếu chưa được confirm.

## Đã làm xong (session gần nhất, đã commit)

**1. Multi-provider data access — tối ưu tốc độ đọc dữ liệu:**
- Phân tích + benchmark thật: xác định `DataTable.Load(IDataReader)` + `PropertyInfo.SetValue` (reflection) là 2 điểm nghẽn chính khi đọc hàng trăm ngàn dòng.
- Thêm `CompiledReaderMapper` (`ApiCore8.Infrastructure/Database/`) — dùng Expression Tree biên dịch 1 lần/lần gọi, đọc thẳng `IDataReader` không qua `DataTable`. Nhanh hơn ~2x thật (đo bằng Stopwatch, tách khỏi JSON serialize) trên data 100k dòng.
- Thêm song song `ExecStoreToListObjectFastAsync<T>` (Postgres/Oracle/SqlServer) — **không thay thế** `ExecStoreToListObjectAsync<T>` cũ, để so sánh A/B qua `GetAllFastAsync`/`/Users/Fast` vs `/Users` (cũ).
- Bug thật bắt được qua unit test: `CompiledReaderMapper` bản đầu dùng `MemberInit` khiến cột `DBNull` ghi đè mất field initializer (VD `Username = string.Empty` → `null`) — fix bằng `Block` + `IfThen` (chỉ gán khi không null), khớp hành vi `DataRowMapper`.
- Thêm `Stopwatch` + `Log.Information("[Bench] ...")` trong `UserRepository.GetAllAsync`/`GetAllFastAsync` để đo tách riêng — **code tạm, gỡ sau khi so sánh xong**.

**2. Dọn `ApiCore8.UnitTests`** (theo "hướng B" — chỉ giữ test cho pure-logic dễ lỗi âm thầm, bỏ test API vì đã test tay qua Postman/Swagger): xóa `SystemLogsControllerTests`, `AddParameterTypeTests`, `DataCoreFactoryTests`, `DateTimeJsonSerializationTests`, `PostgresDbHelperSqlBuildingTests`, `UserRepositoryTests`; giữ `ConnectionStringDetectorTests`, `ExplicitOffsetDateTimeParserTests`, `DataRowMapperTests`, `CancellationTokenTimeoutHelperTests`, thêm mới `CompiledReaderMapperTests` (8 test).

**3. API search cho MongoDB logs (`APILogs` collection):**
- `ApiLogsController` (mới) + `GET /api/ApiLogs/Search?keyword=&fromDate=&toDate=&page=&pageSize=` — search 1 keyword LIKE trên cả 3 field `ApiName`/`RequestBody`/`ResponseBody` (OR), kết hợp AND với khoảng ngày (`fromDate` lọc `StartTime`, `toDate` lọc `EndTime`) — cả keyword và ngày đều optional, độc lập nhau.
- `fromDate`/`toDate` nhận string + bắt buộc offset tường minh qua `ExplicitOffsetDateTimeParser` (tránh lỗi model binder tự quy đổi giờ theo server — đã từng gây "search có data mà ra rỗng").
- Fix lỗi Mongo `MaxDocumentSize` (16MB) khi `[LogApi]` log response quá lớn (VD 100k dòng ≈ 29MB) — thêm `Truncate` (giới hạn 50k ký tự) trong `ApiLoggingAttribute` trước khi ghi `RequestBody`/`ResponseBody` vào Mongo.
- Xóa cột thừa `StartTimeStr`/`EndTimeStr` trong `ApiExecutionLog` (trùng lặp `StartTime`/`EndTime`); thêm `ExecutionTimeDisplay` (string, tự quy đổi đơn vị ms/s/min/h) đi kèm `ExecutionMs` (giữ nguyên kiểu số để `GetSlowLogs` filter/sort được).

**4. Fix "nuốt lỗi" (silent catch) ở tầng repository Mongo:**
- `ApiLogRepository.Search`, `ApiLogRepository.SearchByKeywordAsync`, `ApiLogRepository.GetSlowLogs`, `SystemLogRepository.SearchAsync` — trước đây catch exception rồi trả `PagedResult` rỗng (không có field chứa lỗi) khiến controller không bao giờ đưa được `ex.Message` vào `MessageDetail`. Fix: giữ `Log.Error(...)` (log 1 lần) rồi `throw;` để controller's catch (đã viết đúng sẵn) bắt được và trả lỗi thật cho client.

**5. Rule mới ghi vào `CLAUDE.md`:** mọi action method controller phải trả `Task<APIResult>`, không bao giờ trả thẳng `PagedResult<T>`/DTO ra HTTP response (xác nhận code hiện tại đã tuân thủ đúng).

**6. Data test:** `scripts/postgres_users_seed_1000.sql` — seed 100,000 dòng vào bảng `users` (Postgres) qua `generate_series`, dùng để benchmark thật ở mục 1.

**7. Artifact bản đồ tiến độ (visual):** https://claude.ai/code/artifact/3081c521-23a0-4270-ac0b-518ae5dd0e5c — timeline 5 bước, cùng nội dung với mục "Bản đồ giai đoạn lớn" ở trên; cập nhật lại artifact này (redeploy cùng URL) mỗi khi macro roadmap đổi.

## Việc kế tiếp — pending quyết định của anh

1. **Gỡ code Stopwatch/[Bench] log tạm** trong `UserRepository` sau khi anh so sánh xong tốc độ 2 hàm `GetAllAsync`/`GetAllFastAsync`.
2. Quyết định: giữ cả `ExecStoreToListObjectAsync` (cũ) + `ExecStoreToListObjectFastAsync` (mới) song song, hay thay hẳn toàn bộ repository khác sang bản fast?
3. Oracle/SQL Server: chưa có connection string thật trong User Secrets, chưa verify chạy thật qua Swagger/Postman (mới verify Postgres).
4. `git push` — đang chờ anh confirm (commit `87ba61e` đã có sẵn, chưa rõ đã push hay chưa — kiểm tra lại `git log origin/main` trước khi hỏi).
5. Từ trước, chưa làm: `AntiSpamMiddleware` đang tắt, mask log nhạy cảm (password...) trong `ApiLoggingAttribute`/`ResponseBody`, `RequestTimeouts` middleware + `CancellationToken` cho `ApiLogRepository`/`SystemLogRepository`/`RedisCacheRepository`.
6. Sau đó: bắt đầu bước 4 (Auth Service — Users/OTP/JWT/Session) nếu bước 3 coi như đóng; bước 5 (tích hợp JWT vào Business API) chỉ bắt đầu được sau khi bước 4 xong.

## Cách dùng file này ở session mới

1. Đọc file này trước, không hỏi lại bối cảnh từ đầu.
2. Báo ngắn gọn: "đang ở [mục X trong Đang làm / Việc kế tiếp]" rồi hỏi đúng 1 câu để biết làm tiếp mục nào.
3. Việc nào xong trong session → cập nhật lại đúng file này ngay (không đợi nhắc).
