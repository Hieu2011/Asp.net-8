# Roadmap Auth/SSO (đang ở đầu Giai đoạn 4)

1. **Hạ tầng & CI/CD** — xong phần lớn: Coolify quản container trên VM (laptop cá nhân), Cloudflare Tunnel public app khi ở ngoài mạng công ty, Tailscale cho anh tự vào Coolify Dashboard khi ở công ty (Cloudflare bị chặn). CI/CD: **Coolify webhook tự động khi ở nhà, tự bấm Redeploy qua Tailscale khi ở công ty** — không dùng self-hosted GitHub Actions runner (rủi ro supply-chain trên VM dùng chung nhiều service) và không dùng ngrok (URL đổi liên tục, rủi ro chính sách công ty).
2. **Database** — Postgres mới tạo trên VM, **dùng chung 1 instance nhưng tách riêng database** (`auth_db` cho Auth, database khác cho Business) để giữ ranh giới. Redis riêng cho cache/rate-limit/đếm OTP sai. MongoDB tái dùng cho log (đã có sẵn).
3. **Auth Service** (đang code):
   - Đăng ký: hash password, sinh QR code TOTP (Google Authenticator).
   - Đăng nhập: **Username + OTP là chính**, sai OTP **3 lần liên tiếp** mới fallback bắt nhập Password.
   - Issue JWT (RSA, tự ký — **không dùng Keycloak/OpenIddict**, đã cân nhắc kỹ: Keycloak tốn thêm RAM đáng kể trên VM laptop vốn đã chật, và luồng OTP-trước-Password-sau không khớp mặc định của Keycloak).
   - Quản lý session: lưu thiết bị/IP/thời gian đăng nhập, cho phép liệt kê + thu hồi session hoặc vô hiệu hóa cả user.
4. **Business API** — sau khi Auth xong: thêm JWT Bearer validation bằng RSA public key của Auth Service, gắn `[Authorize]`, bật rate limit built-in .NET 8 (thay `AntiSpamMiddleware` đang tắt).
