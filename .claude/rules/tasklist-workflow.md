# Quản lý công việc qua TaskList

`TaskList/` ở gốc repo — **không commit** (ẩn bằng `.git/info/exclude`, không hiện ở Fork). Thay hoàn toàn `PROJECT_STATUS.md`/`PLAN.md` cũ.

```
TaskList/
  plan.md                       # tổng quan: bảng task theo từng src + trạng thái
  Infra/ ApiCore8/ AuthService/ Shared/ Frontend/ Common/
    <ID>-<ten-task>/task.md     # chi tiết: mô tả, plan checklist, ghi chú
  AuthService/_design.md        # thiết kế tổng Auth (PLAN.md cũ)
  <Src>/_scripts/              # script DB (bảng, store, seed) của từng src
```

- ID theo src: `INF-xx`, `API-xx`, `AUTH-xx`, `SHR-xx`, `FE-xx`, `CMN-xx` (CMN = áp dụng toàn repo).
- Trạng thái: ✅ Xong · 🔄 Đang làm · ⏳ Chưa làm · 🔒 Chờ quyết định · ⏸ Tạm hoãn.

## Quy trình

1. **Đầu session**: đọc `TaskList/plan.md` → mở `task.md` của task liên quan. Không hỏi lại bối cảnh đã có trong đó.
2. **Việc mới**: rã thành task nhỏ, tạo `task.md` (mô tả, plan checklist, ngày tạo) + thêm dòng vào `plan.md`.
3. **Trong lúc làm**: tick checklist, ghi ngày cập nhật; điều đáng lưu ý (bug, quyết định, bài học không suy ra được từ code) ghi vào mục "Ghi chú" của `task.md`.
4. **Hoàn thành**: chỉ đánh ✅ ở `plan.md` khi anh confirm xong.
5. **Hook tự động** (`.claude/settings.json`):
   - `SessionStart`: nạp `TaskList/plan.md` vào context (PowerShell, chỉ đọc) — không cần tự mở lại.
   - `SessionEnd` (`.claude/hooks/session-end-log.js`): ghi 1 dòng tóm tắt (yêu cầu gần nhất + file đang đổi) vào mục "Nhật ký session" cuối `plan.md`, giữ 20 dòng. **Không** đổi trạng thái task — cập nhật `task.md` và đánh ✅ vẫn làm theo bước 3–4.
6. Bài học đã thành quy ước ổn định → chuyển vào `.claude/rules/*.md`, xóa khỏi task.
