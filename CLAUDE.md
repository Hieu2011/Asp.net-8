# WebApiCore8 / AuthService — Project Notes

Monorepo gồm 3 src backend trong `src/`: `ApiCore8` (Business API), `AuthService` (Auth/SSO), `Shared` (thư viện dùng chung) + `frontend/` (Next.js 16, web bán hàng — sau này tách repo). **Rule code dùng chung cho cả 3 src backend, frontend có rule riêng cùng tinh thần; TaskList tách riêng theo từng src.**

**Đầu mỗi session: đọc `TaskList/plan.md` trước** (không commit, chỉ có trên máy anh) rồi mở `task.md` của task liên quan.

Rule chi tiết trong `.claude/rules/` (tự động load):

- `working-agreements.md` — cách làm việc, tiết kiệm token, unit test, git
- `coding-standards.md` — tiền tố biến, thứ tự member, vị trí code, async/đa luồng (tối đa 5), comment, response API
- `frontend-standards.md` — rule `frontend/` (tiền tố biến giống backend, cấu trúc feature, server/client component, state, ESLint/Prettier)
- `tasklist-workflow.md` — cấu trúc + quy trình TaskList
- `solution-structure.md` — cấu trúc solution, Clean Architecture, Shared
- `roadmap-auth-sso.md` — quyết định kiến trúc Auth/SSO
- `cleanup-log.md` — đã dọn dẹp, không đề xuất lại
