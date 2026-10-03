---
name: deploy
description: Merge nhánh main vào GIT/production hoặc GIT/staging theo đúng quy trình CI/CD (--no-commit + commit message bắt buộc chứa "up pro"/"up staging"), rồi push sau khi user xác nhận. Dùng khi user muốn deploy lên production/staging.
---

# Deploy lên Production/Staging

Tham số: `production` hoặc `staging` (đọc từ lệnh gọi, VD `/deploy production`). Nếu không rõ target, hỏi lại trước khi làm gì.

1. Xác định nhánh đích: `production` → `GIT/production`, `staging` → `GIT/staging`.
2. `git status` — nếu có thay đổi chưa commit ở nhánh hiện tại, dừng lại, hỏi user xử lý sao (commit/stash) trước khi tiếp tục.
3. `git checkout <nhánh đích>`
4. `git pull` — đồng bộ nhánh đích với remote trước khi merge, tránh merge trên bản cũ.
5. `git merge main --no-commit` — merge từ `main`, không tự commit (giữ đúng thói quen anh dùng qua Fork).
6. Nếu conflict:
   - Conflict cơ học (2 bên sửa 2 vùng không liên quan) → tự resolve, show lại đoạn đã sửa để user xác nhận trước khi đi tiếp.
   - Conflict logic/business (cùng đoạn code, ý đồ khác nhau) → **không tự quyết** — trình bày rõ 2 phiên bản khác nhau ở đâu, đề xuất hướng, hỏi user chọn.
7. Soạn sẵn commit message — **bắt buộc chứa đúng cụm** `"up pro"` (production) hoặc `"up staging"` (staging), không sai chính tả/thiếu khoảng trắng. Show cho user xem trước khi commit.
8. Hỏi xác nhận rõ ràng trước khi `git commit` + `git push` — không tự ý push (theo `.claude/rules/working-agreements.md`).
9. Sau khi push, nhắc user: GitHub Actions sẽ tự trigger theo commit message, xem trạng thái bằng `gh run list --workflow=deploy.yml` (production) hoặc `--workflow=deploy-staging.yml` (staging) nếu có `gh` CLI, hoặc mở tab Actions trên GitHub.
