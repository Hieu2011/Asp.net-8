# Cách làm việc đã thống nhất

## Trả lời & token
- Trả lời đúng trọng tâm câu hỏi, không lan man, không lặp lại đề bài.
- Tiết kiệm token tối đa: không đọc file thừa, không dump output dài, không giải thích hiển nhiên.
- Tìm/hiểu code: dùng **CodeGraph** (`codegraph_explore`) trước tiên. **Không dùng subagent tìm kiếm** (`Explore`, `general-purpose`, `Plan`) — CodeGraph đã đủ.
- Subagent còn giữ: `code-reviewer` (review diff trước merge), `security-reviewer` (code Auth/secret/permission) — chỉ gọi khi anh yêu cầu hoặc trước khi tạo PR.

## Code & verify
- Thay đổi lớn/nhiều file: trình plan trước (ghi vào `task.md`), đợi duyệt rồi mới code.
- **Unit test: không tự viết/chạy** — chỉ làm khi anh yêu cầu cụ thể. Sau khi sửa code chỉ cần `dotnet build` solution liên quan (0 Error) để verify. Rule này ghi đè mục "Test & Build" ở CLAUDE.md global.
- Giải thích khái niệm .NET Core mới (DI, async, tuple...) từ bản chất kèm ví dụ — anh đang chuyển từ .NET Framework.

## Git
- **Chỉ sửa file, không tự ý `git commit`** — để ở working tree, đợi anh yêu cầu rõ ràng.
- Trước `git push`: hỏi xác nhận.
