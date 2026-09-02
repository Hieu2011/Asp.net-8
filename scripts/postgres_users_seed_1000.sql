-- Seed 100,000 dòng test vào bảng users (Postgres) — dùng generate_series, 1 câu insert duy nhất.
-- Chạy trực tiếp trên auth_db/database chứa bảng users, KHÔNG qua sp_user_create (để tránh
-- 100k round-trip riêng lẻ qua function/refcursor — chỉ cần dữ liệu test, không cần qua tầng app).
-- 1000 dòng quá ít để thấy chênh lệch DataTable.Load vs đọc thẳng reader (chênh lệch bị nhiễu bởi
-- network/JIT) — cần quy mô chục-trăm ngàn dòng mới lộ rõ bản chất chi phí buffer/reflection.

INSERT INTO users (username, password_hash, full_name, email, is_active, created_at, updated_at)
SELECT
    'testuser' || i,
    'hash_' || md5('testuser' || i),
    'Test User ' || i,
    'testuser' || i || '@example.com',
    (i % 10 <> 0), -- ~10% is_active = false, còn lại true
    now() - (random() * interval '365 days'),
    now()
FROM generate_series(1, 100000) AS s(i);
