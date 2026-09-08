# Bản đồ tiến độ — đọc file này đầu tiên khi bắt đầu session mới

File này có 2 phần, khác nhau về cách cập nhật:

- **"Đang ở đâu"** — phản ánh hiện tại, sửa/xóa liên tục. Việc gì xong và đã commit thì xóa khỏi đây (git log lo phần lịch sử "đổi gì").
- **"Nhật ký quyết định & sự cố quan trọng"** — **không xóa theo commit**, chỉ thêm dòng mới. Đây là chỗ git log không thay được: lưu *tại sao*/*đã sai ở đâu rồi sửa sao*/*đã hỏi gì*, để session sau không hỏi lại hoặc lặp lại lỗi cũ.
  - Chỉ ghi bài học **không tự suy ra được** từ code/git diff — không chép lại nội dung đã có trong `CLAUDE.md`/`.claude/rules/`.
  - Mỗi dòng tối đa 1-3 câu, không kể lại toàn bộ quá trình debug.
  - Khi 1 bài học "chín" thành quy ước ổn định (được ghi vào `.claude/rules/*.md`) → **xóa khỏi log này**, tránh giữ 2 nơi.
  - Nếu log này dài quá dù đã áp 2 quy tắc trên → tách phần cũ/đã xong hẳn sang `PROJECT_STATUS_ARCHIVE.md` (không đọc mỗi session, chỉ tra khi cần).

## Đang ở đâu

- [x] **Bước 1 — Hạ tầng & CI/CD**: Coolify + Cloudflare Tunnel + Tailscale chạy thật. CI/CD đã triển khai và **verify thành công thật**: push vào `GIT/production`/`GIT/staging` kèm đúng cú pháp commit message (`up pro`/`up staging`) → GitHub Actions tự gọi Coolify Deploy Webhook API — đã confirm deploy thành công qua log Coolify.
- [x] **Bước 2 — Database**: Postgres (`auth_db` riêng), Redis riêng, MongoDB tái dùng cho log.
- [~] **Bước 3 — Dọn dẹp & tối ưu WebApiCore8**: Clean Architecture, multi-provider data access, logging Mongo + API search đã xong. Còn sót — xem `.claude/rules/known-issues.md`.
- [ ] **Bước 4 — Auth Service**: mới ở mức skeleton project, chưa code business logic (Users/OTP/JWT/Session). Chưa bắt đầu.
- [ ] **Bước 5 — Business API: tích hợp JWT**: phụ thuộc bước 4, chưa bắt đầu.

→ **Đang ở bước 3, bước 1 đã xong hẳn.**

**Git:** branch làm việc `GIT/staging` (merge từ `main` qua Fork, chọn "Don't Commit" để gõ tay commit message `up staging`/`up pro`). Working tree clean.

## Việc kế tiếp — pending quyết định của anh

1. Quyết định: giữ cả `ExecStoreToListObjectAsync` (cũ) + `ExecStoreToListObjectFastAsync` (mới) song song, hay thay hẳn toàn bộ repository sang bản fast? (đang có code `Stopwatch`/`[Bench]` tạm trong `UserRepository` để so sánh — gỡ sau khi anh chốt.)
2. Oracle/SQL Server: chưa có connection string thật trong User Secrets, chưa verify chạy thật (mới verify Postgres).
3. Xác nhận **Auto Deploy đã tắt trên Coolify cho app staging** (production đã confirm tắt).
4. Test lại Coolify **Rollback** sau khi tắt "Shallow Clone" (Advanced settings) — xem mục sự cố bên dưới, chưa xác nhận đã tắt/test.
5. `COOLIFY_STAGING_UUID` — xác nhận đã tạo GitHub Secret hay chưa.
6. Sau khi bước 3 coi như đóng: bắt đầu bước 4 (Auth Service).

## Nhật ký quyết định & sự cố quan trọng

- **CI/CD gating theo commit message**: chỉ deploy khi commit message chứa đúng `"up pro"`/`"up staging"`; sai cú pháp → job GitHub Actions phải **FAIL đỏ** (dùng `exit 1`), không dùng `if: contains(...)` ở job-level vì nó chỉ hiện "Skipped" xám, không rõ nguyên nhân.
- **Coolify "Auto Deploy" (Advanced tab) phải tắt thủ công** cho từng Application — mặc định nó ON và tự deploy trên MỌI push bất kể commit message, chạy song song gây deploy trùng với pipeline gate ở trên.
- **Dockerfile phải đặt ở root repo**, tên `Dockerfile.<service>` (VD `Dockerfile.apicore8`), Coolify Base Directory = `/`, Dockerfile Location = tên file không kèm `/` — đặt Dockerfile trong subfolder (`ApiCore8.Api/Dockerfile`) làm Coolify tự `mkdir -p` trùng path với file git đã clone → lỗi "File exists". Áp dụng convention này cho mọi service tương lai (AuthService...).
- **Coolify Rollback tab có bug đã biết** (xác nhận qua log thật + GitHub issue #1976/#8445 của coollabsio/coolify): deploy/rollback luôn `git clone --depth=1` (chỉ lấy tip nhánh hiện tại), nên rollback về bất kỳ commit nào khác tip đều lỗi `fatal: bad object`. Workaround cộng đồng đề xuất: tắt "Shallow Clone" trong Advanced settings — **nhưng issue #8445 cảnh báo tắt xong có thể chỉ hết crash mà rollback lại âm thầm không đổi code thật** → phải test lại bằng cách verify code chạy thực tế sau rollback, không chỉ tin "deploy thành công" là đủ.
- **Fork (Git GUI) merge option đúng cho workflow "gõ up pro/up staging"**: phải chọn **"Don't Commit"** (`--no-commit`) chứ không phải "No Fast-Forward" — No Fast-Forward tự commit ngay với message mặc định, không cho sửa tay.
