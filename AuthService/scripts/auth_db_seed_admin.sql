-- ============================================================
-- Seed Admin đầu tiên (RoleLevel=1) — chạy TAY 1 LẦN lúc setup DB, KHÔNG qua API
-- (mục 8 PLAN.md — trứng-gà problem: chưa có ai cấp cao hơn để tạo Admin đầu tiên qua endpoint).
--
-- File này KHÔNG chạy được nguyên si — 2 giá trị dưới đây phải tự sinh THỦ CÔNG trước,
-- vì password_hash (BCrypt) và totp_secret (mã hóa AES theo Auth:TotpEncryptionKey) đều
-- cần logic C# của chính AuthService, SQL thuần không tính ra được các giá trị này.
--
-- CÁCH LẤY 2 GIÁ TRỊ:
--   1) password_hash: viết 1 unit test/console tạm gọi BCrypt.Net.BCrypt.HashPassword("<mật khẩu Admin>")
--   2) totp_secret (đã mã hóa): gọi ITotpService sinh 1 secret ngẫu nhiên (Otp.NET),
--      rồi gọi ITotpSecretEncryption.Encrypt(secret) — copy secret GỐC (chưa mã hóa) ra riêng
--      để nhập tay vào Google Authenticator, chỉ giá trị ĐÃ MÃ HÓA mới insert vào cột totp_secret.
--
-- Sau khi chạy xong: đăng nhập thử /auth/login bằng OTP từ Google Authenticator để xác nhận
-- secret đã mã hóa/giải mã đúng, rồi ĐỔI PASSWORD ngay qua /auth/password/forgot (mục 36)
-- thay vì giữ mật khẩu tạm đã gõ tay ở bước 1.
-- ============================================================

INSERT INTO users (
    username, email, password_hash, totp_secret, is_totp_enabled,
    role_level, is_active, is_permanently_locked,
    created_date, created_user
) VALUES (
    'admin',                                   -- đổi username thật nếu muốn
    'admin@dienmayxanh.com',                   -- đổi email thật — bắt buộc, dùng cho luồng quên mật khẩu (mục 36)
    '<BCRYPT_HASH_CUA_MAT_KHAU_ADMIN>',        -- thay bằng hash sinh ở bước 1
    '<TOTP_SECRET_DA_MA_HOA_AES>',              -- thay bằng giá trị đã mã hóa ở bước 2
    true,
    1,                                          -- RoleLevel.Admin (mục 2)
    true,
    false,
    now(), 'system_seed'
);

-- Kiểm tra lại sau khi chạy:
-- SELECT id, username, email, role_level, is_active FROM users WHERE username = 'admin';
