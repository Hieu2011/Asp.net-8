-- ============================================================
-- Postgres (auth_db) — bảng + stored function cho AuthService.
-- Theo đúng convention ở scripts/postgres_users_setup.sql:
--   - v_out REFCURSOR (Npgsql đọc qua OPEN/FETCH/CLOSE trong 1 transaction,
--     PostgresDbHelper.ExecuteStoreDataTableAsync tự mở transaction cục bộ nếu chưa có).
--   - snake_case, UUID PK gen_random_uuid(), TIMESTAMPTZ (UTC — C# luôn truyền UTC).
--   - Soft delete: is_deleted/deleted_date/deleted_user — mọi sp_*_get_* phải WHERE is_deleted = false.
--   - App chỉ có quyền EXECUTE function, không SELECT/INSERT/UPDATE/DELETE trực tiếp trên bảng
--     (xem block REVOKE/GRANT cuối file, thay <APP_DB_USER> bằng user Postgres thật app connect).
--
-- Tham chiếu quyết định trong AuthService/PLAN.md — mục số ghi trong comment map thẳng tới bảng
-- "Quyết định đã chốt" (mục 1) để tra lại lý do khi cần.
-- ============================================================

-- ============================================================
-- 1) BẢNG users
-- ============================================================
CREATE TABLE users (
    id                        UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    username                  VARCHAR(100) NOT NULL,
    email                     VARCHAR(255) NOT NULL,
    password_hash             VARCHAR(255),                 -- NULL tạm thời cho user mới qua Google chưa hoàn tất setup (mục 46)
    google_id                 VARCHAR(50),                   -- sub claim Google ID Token, NULL nếu chưa liên kết (mục 45)
    totp_secret               TEXT,                          -- mã hóa AES trước khi lưu (mục 13) — ứng dụng tự mã hóa/giải mã, DB chỉ lưu chuỗi đã mã hóa
    is_totp_enabled           BOOLEAN NOT NULL DEFAULT false,
    role_level                SMALLINT NOT NULL,             -- 1=Admin,2=Director,3=Manager,4=Staff,5=Customer (mục 2)
    failed_otp_attempt_count  INT NOT NULL DEFAULT 0,        -- audit/hiển thị — Redis là nguồn đúng cho logic khóa (mục 17-18)
    is_active                 BOOLEAN NOT NULL DEFAULT true,
    is_permanently_locked     BOOLEAN NOT NULL DEFAULT false, -- mục 35, chỉ gỡ qua luồng reset email (mục 36) hoặc Admin (mục 37)

    created_date   TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_user   VARCHAR(50),
    updated_date   TIMESTAMPTZ,
    updated_user   VARCHAR(50),
    is_deleted     BOOLEAN NOT NULL DEFAULT false,
    deleted_date   TIMESTAMPTZ,
    deleted_user   VARCHAR(50)
);

-- UNIQUE dạng partial index (WHERE is_deleted = false) thay vì UNIQUE constraint cứng trên cột:
-- soft-delete 1 user rồi vẫn cho phép username/email/google_id đó được dùng lại cho user mới.
CREATE UNIQUE INDEX ux_users_username   ON users (username)  WHERE is_deleted = false;
CREATE UNIQUE INDEX ux_users_email      ON users (email)     WHERE is_deleted = false;
CREATE UNIQUE INDEX ux_users_google_id  ON users (google_id) WHERE is_deleted = false AND google_id IS NOT NULL;

-- ============================================================
-- 2) BẢNG sessions
-- ============================================================
CREATE TABLE sessions (
    id                            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id                       UUID NOT NULL REFERENCES users(id),
    device_info                   TEXT,
    ip_address                    VARCHAR(45),                -- hỗ trợ IPv6
    refresh_token_hash            VARCHAR(255) NOT NULL,       -- SHA256 (mục 31, không dùng BCrypt)
    previous_refresh_token_hash   VARCHAR(255),                -- reuse detection (mục 20)
    last_activity_at              TIMESTAMPTZ NOT NULL DEFAULT now(),
    expires_at                    TIMESTAMPTZ NOT NULL,        -- giới hạn tuyệt đối, không gia hạn quá mốc này (mục 21)
    is_revoked                    BOOLEAN NOT NULL DEFAULT false,

    created_date   TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_user   VARCHAR(50),
    updated_date   TIMESTAMPTZ,
    updated_user   VARCHAR(50),
    is_deleted     BOOLEAN NOT NULL DEFAULT false,
    deleted_date   TIMESTAMPTZ,
    deleted_user   VARCHAR(50)
);

CREATE INDEX idx_sessions_user_id ON sessions (user_id) WHERE is_deleted = false;
CREATE UNIQUE INDEX ux_sessions_refresh_token_hash ON sessions (refresh_token_hash) WHERE is_deleted = false;
CREATE INDEX idx_sessions_previous_refresh_token_hash ON sessions (previous_refresh_token_hash) WHERE is_deleted = false;

-- ============================================================
-- 3) BẢNG clients (đối tác ngoài, Flow B — OAuth2 Client Credentials)
-- ============================================================
CREATE TABLE clients (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id           VARCHAR(100) NOT NULL,
    client_name         VARCHAR(200) NOT NULL,
    client_secret_hash  VARCHAR(255) NOT NULL,        -- BCrypt
    allowed_scopes      TEXT[] NOT NULL DEFAULT '{}',
    allowed_ips         TEXT[],                        -- NULL = không giới hạn IP (mục 23)
    is_active           BOOLEAN NOT NULL DEFAULT true,

    created_date   TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_user   VARCHAR(50),
    updated_date   TIMESTAMPTZ,
    updated_user   VARCHAR(50),
    is_deleted     BOOLEAN NOT NULL DEFAULT false,
    deleted_date   TIMESTAMPTZ,
    deleted_user   VARCHAR(50)
);

CREATE UNIQUE INDEX ux_clients_client_id ON clients (client_id) WHERE is_deleted = false;

-- ============================================================
-- 4) BẢNG login_history (ghi mãi mãi, KHÔNG soft-delete — khác sessions)
-- ============================================================
CREATE TABLE login_history (
    id                   UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id              UUID REFERENCES users(id),     -- NULL nếu login thất bại với username không tồn tại
    username_attempted   VARCHAR(100) NOT NULL,
    ip_address           VARCHAR(45),
    device_info          TEXT,
    login_type           VARCHAR(20) NOT NULL,           -- otp / password_fallback / refresh / password_reset
    is_success           BOOLEAN NOT NULL,
    failure_reason       VARCHAR(200),                   -- NULL nếu thành công
    created_date         TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX idx_login_history_user_id ON login_history (user_id);
CREATE INDEX idx_login_history_created_date ON login_history (created_date);
CREATE INDEX idx_login_history_username_attempted ON login_history (username_attempted);


-- ============================================================
-- 5) FUNCTIONS — users
-- ============================================================

-- Tạo user mới. v_password_hash/v_totp_secret cho phép NULL (luồng Google mới, mục 46).
CREATE OR REPLACE FUNCTION sp_user_create(
    INOUT v_out refcursor,
    v_username varchar,
    v_email varchar,
    v_password_hash varchar,
    v_totp_secret text,
    v_role_level smallint,
    v_google_id varchar,
    v_created_user varchar
) AS $$
BEGIN
    OPEN v_out FOR
    INSERT INTO users (username, email, password_hash, totp_secret, is_totp_enabled,
                        role_level, google_id, is_active, is_permanently_locked,
                        created_date, created_user)
    VALUES (v_username, v_email, v_password_hash, v_totp_secret, (v_totp_secret IS NOT NULL),
            v_role_level, v_google_id, true, false,
            now(), v_created_user)
    RETURNING id, username, email, password_hash, totp_secret, is_totp_enabled, role_level,
              failed_otp_attempt_count, is_active, is_permanently_locked, google_id,
              created_date, created_user, updated_date, updated_user,
              is_deleted, deleted_date, deleted_user;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION sp_user_get_by_id(INOUT v_out refcursor, v_id uuid) AS $$
BEGIN
    OPEN v_out FOR
    SELECT id, username, email, password_hash, totp_secret, is_totp_enabled, role_level,
           failed_otp_attempt_count, is_active, is_permanently_locked, google_id,
           created_date, created_user, updated_date, updated_user,
           is_deleted, deleted_date, deleted_user
    FROM users WHERE id = v_id AND is_deleted = false;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION sp_user_get_by_username(INOUT v_out refcursor, v_username varchar) AS $$
BEGIN
    OPEN v_out FOR
    SELECT id, username, email, password_hash, totp_secret, is_totp_enabled, role_level,
           failed_otp_attempt_count, is_active, is_permanently_locked, google_id,
           created_date, created_user, updated_date, updated_user,
           is_deleted, deleted_date, deleted_user
    FROM users WHERE username = v_username AND is_deleted = false;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION sp_user_get_by_email(INOUT v_out refcursor, v_email varchar) AS $$
BEGIN
    OPEN v_out FOR
    SELECT id, username, email, password_hash, totp_secret, is_totp_enabled, role_level,
           failed_otp_attempt_count, is_active, is_permanently_locked, google_id,
           created_date, created_user, updated_date, updated_user,
           is_deleted, deleted_date, deleted_user
    FROM users WHERE email = v_email AND is_deleted = false;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION sp_user_get_by_google_id(INOUT v_out refcursor, v_google_id varchar) AS $$
BEGIN
    OPEN v_out FOR
    SELECT id, username, email, password_hash, totp_secret, is_totp_enabled, role_level,
           failed_otp_attempt_count, is_active, is_permanently_locked, google_id,
           created_date, created_user, updated_date, updated_user,
           is_deleted, deleted_date, deleted_user
    FROM users WHERE google_id = v_google_id AND is_deleted = false;
END;
$$ LANGUAGE plpgsql;

-- Danh sách phân trang, lọc theo role_level (NULL = không lọc) — dùng cho GET /auth/users.
CREATE OR REPLACE FUNCTION sp_user_get_paged(
    INOUT v_out refcursor,
    v_role_level smallint,
    v_page int,
    v_page_size int
) AS $$
BEGIN
    OPEN v_out FOR
    SELECT id, username, email, role_level, is_active, is_permanently_locked, is_totp_enabled,
           google_id, created_date, updated_date
    FROM users
    WHERE is_deleted = false
      AND (v_role_level IS NULL OR role_level = v_role_level)
    ORDER BY created_date DESC
    OFFSET GREATEST(v_page - 1, 0) * v_page_size LIMIT v_page_size;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION sp_user_update_password(
    v_id uuid, v_password_hash varchar, v_updated_user varchar
) RETURNS boolean AS $$
DECLARE affected int;
BEGIN
    UPDATE users SET password_hash = v_password_hash, updated_date = now(), updated_user = v_updated_user
    WHERE id = v_id AND is_deleted = false;
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected > 0;
END;
$$ LANGUAGE plpgsql;

-- Sinh lại/set TOTP secret (đăng ký, reset password mục 36, hoàn tất setup Google mục 46).
CREATE OR REPLACE FUNCTION sp_user_update_totp_secret(
    v_id uuid, v_totp_secret text, v_is_totp_enabled boolean, v_updated_user varchar
) RETURNS boolean AS $$
DECLARE affected int;
BEGIN
    UPDATE users SET totp_secret = v_totp_secret, is_totp_enabled = v_is_totp_enabled,
                      updated_date = now(), updated_user = v_updated_user
    WHERE id = v_id AND is_deleted = false;
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected > 0;
END;
$$ LANGUAGE plpgsql;

-- Liên kết google_id vào user có sẵn (email trùng, mục 45) — KHÔNG đổi role/password.
CREATE OR REPLACE FUNCTION sp_user_link_google(
    v_id uuid, v_google_id varchar, v_updated_user varchar
) RETURNS boolean AS $$
DECLARE affected int;
BEGIN
    UPDATE users SET google_id = v_google_id, updated_date = now(), updated_user = v_updated_user
    WHERE id = v_id AND is_deleted = false;
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected > 0;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION sp_user_update_role(
    v_id uuid, v_role_level smallint, v_updated_user varchar
) RETURNS boolean AS $$
DECLARE affected int;
BEGIN
    UPDATE users SET role_level = v_role_level, updated_date = now(), updated_user = v_updated_user
    WHERE id = v_id AND is_deleted = false;
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected > 0;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION sp_user_set_active(
    v_id uuid, v_is_active boolean, v_updated_user varchar
) RETURNS boolean AS $$
DECLARE affected int;
BEGIN
    UPDATE users SET is_active = v_is_active, updated_date = now(), updated_user = v_updated_user
    WHERE id = v_id AND is_deleted = false;
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected > 0;
END;
$$ LANGUAGE plpgsql;

-- true = khóa vĩnh viễn (mục 35 escalation); false = gỡ khóa (mục 36 self-service / mục 37 Admin).
CREATE OR REPLACE FUNCTION sp_user_set_permanently_locked(
    v_id uuid, v_is_permanently_locked boolean, v_updated_user varchar
) RETURNS boolean AS $$
DECLARE affected int;
BEGIN
    UPDATE users SET is_permanently_locked = v_is_permanently_locked,
                      updated_date = now(), updated_user = v_updated_user
    WHERE id = v_id AND is_deleted = false;
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected > 0;
END;
$$ LANGUAGE plpgsql;

-- Đồng bộ cột audit failed_otp_attempt_count theo counter Redis (Redis vẫn là nguồn đúng cho logic khóa).
CREATE OR REPLACE FUNCTION sp_user_set_failed_otp_count(
    v_id uuid, v_count int
) RETURNS boolean AS $$
DECLARE affected int;
BEGIN
    UPDATE users SET failed_otp_attempt_count = v_count, updated_date = now()
    WHERE id = v_id AND is_deleted = false;
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected > 0;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION sp_user_soft_delete(
    v_id uuid, v_deleted_user varchar
) RETURNS boolean AS $$
DECLARE affected int;
BEGIN
    UPDATE users SET is_deleted = true, deleted_date = now(), deleted_user = v_deleted_user
    WHERE id = v_id AND is_deleted = false;
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected > 0;
END;
$$ LANGUAGE plpgsql;


-- ============================================================
-- 6) FUNCTIONS — sessions
-- ============================================================

CREATE OR REPLACE FUNCTION sp_session_create(
    INOUT v_out refcursor,
    v_user_id uuid,
    v_device_info text,
    v_ip_address varchar,
    v_refresh_token_hash varchar,
    v_expires_at timestamptz,
    v_created_user varchar
) AS $$
BEGIN
    OPEN v_out FOR
    INSERT INTO sessions (user_id, device_info, ip_address, refresh_token_hash,
                           last_activity_at, expires_at, is_revoked,
                           created_date, created_user)
    VALUES (v_user_id, v_device_info, v_ip_address, v_refresh_token_hash,
            now(), v_expires_at, false,
            now(), v_created_user)
    RETURNING id, user_id, device_info, ip_address, refresh_token_hash, previous_refresh_token_hash,
              last_activity_at, expires_at, is_revoked,
              created_date, created_user, updated_date, updated_user,
              is_deleted, deleted_date, deleted_user;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION sp_session_get_by_id(INOUT v_out refcursor, v_id uuid) AS $$
BEGIN
    OPEN v_out FOR
    SELECT id, user_id, device_info, ip_address, refresh_token_hash, previous_refresh_token_hash,
           last_activity_at, expires_at, is_revoked,
           created_date, created_user, updated_date, updated_user,
           is_deleted, deleted_date, deleted_user
    FROM sessions WHERE id = v_id AND is_deleted = false;
END;
$$ LANGUAGE plpgsql;

-- Tìm session khớp v_hash ở CẢ 2 cột — match_type trả về để app biết là 'current' (refresh hợp lệ)
-- hay 'previous' (refresh token đã bị rotate rồi mà còn bị dùng lại → nghi bị đánh cắp, mục 20).
CREATE OR REPLACE FUNCTION sp_session_find_by_refresh_hash(
    INOUT v_out refcursor, v_hash varchar
) AS $$
BEGIN
    OPEN v_out FOR
    SELECT id, user_id, device_info, ip_address, refresh_token_hash, previous_refresh_token_hash,
           last_activity_at, expires_at, is_revoked,
           CASE WHEN refresh_token_hash = v_hash THEN 'current' ELSE 'previous' END AS match_type
    FROM sessions
    WHERE is_deleted = false
      AND (refresh_token_hash = v_hash OR previous_refresh_token_hash = v_hash);
END;
$$ LANGUAGE plpgsql;

-- Rotation: refresh_token_hash cũ dồn xuống previous_refresh_token_hash, gắn hash mới (mục 20).
CREATE OR REPLACE FUNCTION sp_session_rotate_refresh_token(
    v_id uuid, v_new_refresh_token_hash varchar, v_updated_user varchar
) RETURNS boolean AS $$
DECLARE affected int;
BEGIN
    UPDATE sessions
    SET previous_refresh_token_hash = refresh_token_hash,
        refresh_token_hash = v_new_refresh_token_hash,
        last_activity_at = now(),
        updated_date = now(), updated_user = v_updated_user
    WHERE id = v_id AND is_deleted = false AND is_revoked = false;
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected > 0;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION sp_session_get_active_by_user(
    INOUT v_out refcursor, v_user_id uuid
) AS $$
BEGIN
    OPEN v_out FOR
    SELECT id, user_id, device_info, ip_address, last_activity_at, expires_at, is_revoked, created_date
    FROM sessions
    WHERE user_id = v_user_id AND is_deleted = false AND is_revoked = false AND expires_at > now()
    ORDER BY last_activity_at DESC;
END;
$$ LANGUAGE plpgsql;

-- Admin xem TẤT CẢ session đang active toàn hệ thống (GET /auth/admin/sessions).
CREATE OR REPLACE FUNCTION sp_session_get_all_active(INOUT v_out refcursor) AS $$
BEGIN
    OPEN v_out FOR
    SELECT s.id, s.user_id, u.username, s.device_info, s.ip_address,
           s.last_activity_at, s.expires_at, s.is_revoked, s.created_date
    FROM sessions s
    JOIN users u ON u.id = s.user_id AND u.is_deleted = false
    WHERE s.is_deleted = false AND s.is_revoked = false AND s.expires_at > now()
    ORDER BY s.last_activity_at DESC;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION sp_session_revoke(
    v_id uuid, v_updated_user varchar
) RETURNS boolean AS $$
DECLARE affected int;
BEGIN
    UPDATE sessions SET is_revoked = true, updated_date = now(), updated_user = v_updated_user
    WHERE id = v_id AND is_deleted = false AND is_revoked = false;
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected > 0;
END;
$$ LANGUAGE plpgsql;

-- Revoke toàn bộ session của 1 user — dùng cho: tự logout all (mục 33), đổi role (mục 34),
-- disable/delete user, reset password (mục 42). Trả về SỐ session vừa bị revoke.
CREATE OR REPLACE FUNCTION sp_session_revoke_all_by_user(
    v_user_id uuid, v_updated_user varchar
) RETURNS int AS $$
DECLARE affected int;
BEGIN
    UPDATE sessions SET is_revoked = true, updated_date = now(), updated_user = v_updated_user
    WHERE user_id = v_user_id AND is_deleted = false AND is_revoked = false;
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$ LANGUAGE plpgsql;


-- ============================================================
-- 7) FUNCTIONS — clients (Flow B)
-- ============================================================

CREATE OR REPLACE FUNCTION sp_client_create(
    INOUT v_out refcursor,
    v_client_id varchar,
    v_client_name varchar,
    v_client_secret_hash varchar,
    v_allowed_scopes text[],
    v_allowed_ips text[],
    v_created_user varchar
) AS $$
BEGIN
    OPEN v_out FOR
    INSERT INTO clients (client_id, client_name, client_secret_hash, allowed_scopes, allowed_ips,
                          is_active, created_date, created_user)
    VALUES (v_client_id, v_client_name, v_client_secret_hash, v_allowed_scopes, v_allowed_ips,
            true, now(), v_created_user)
    RETURNING id, client_id, client_name, client_secret_hash, allowed_scopes, allowed_ips,
              is_active, created_date, created_user, updated_date, updated_user,
              is_deleted, deleted_date, deleted_user;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION sp_client_get_by_client_id(
    INOUT v_out refcursor, v_client_id varchar
) AS $$
BEGIN
    OPEN v_out FOR
    SELECT id, client_id, client_name, client_secret_hash, allowed_scopes, allowed_ips,
           is_active, created_date, created_user, updated_date, updated_user,
           is_deleted, deleted_date, deleted_user
    FROM clients WHERE client_id = v_client_id AND is_deleted = false;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION sp_client_get_all(INOUT v_out refcursor) AS $$
BEGIN
    OPEN v_out FOR
    SELECT id, client_id, client_name, allowed_scopes, allowed_ips, is_active, created_date
    FROM clients WHERE is_deleted = false ORDER BY created_date DESC;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION sp_client_regenerate_secret(
    v_id uuid, v_new_secret_hash varchar, v_updated_user varchar
) RETURNS boolean AS $$
DECLARE affected int;
BEGIN
    UPDATE clients SET client_secret_hash = v_new_secret_hash, updated_date = now(), updated_user = v_updated_user
    WHERE id = v_id AND is_deleted = false;
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected > 0;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION sp_client_set_active(
    v_id uuid, v_is_active boolean, v_updated_user varchar
) RETURNS boolean AS $$
DECLARE affected int;
BEGIN
    UPDATE clients SET is_active = v_is_active, updated_date = now(), updated_user = v_updated_user
    WHERE id = v_id AND is_deleted = false;
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected > 0;
END;
$$ LANGUAGE plpgsql;


-- ============================================================
-- 8) FUNCTIONS — login_history (không soft-delete, ghi mãi mãi)
-- ============================================================

CREATE OR REPLACE FUNCTION sp_login_history_insert(
    v_user_id uuid,
    v_username_attempted varchar,
    v_ip_address varchar,
    v_device_info text,
    v_login_type varchar,
    v_is_success boolean,
    v_failure_reason varchar
) RETURNS uuid AS $$
DECLARE new_id uuid;
BEGIN
    INSERT INTO login_history (user_id, username_attempted, ip_address, device_info,
                                login_type, is_success, failure_reason, created_date)
    VALUES (v_user_id, v_username_attempted, v_ip_address, v_device_info,
            v_login_type, v_is_success, v_failure_reason, now())
    RETURNING id INTO new_id;
    RETURN new_id;
END;
$$ LANGUAGE plpgsql;

-- Lịch sử login của 1 user (Admin tra cứu), phân trang.
CREATE OR REPLACE FUNCTION sp_login_history_get_by_user(
    INOUT v_out refcursor, v_user_id uuid, v_page int, v_page_size int
) AS $$
BEGIN
    OPEN v_out FOR
    SELECT id, user_id, username_attempted, ip_address, device_info,
           login_type, is_success, failure_reason, created_date
    FROM login_history
    WHERE user_id = v_user_id
    ORDER BY created_date DESC
    OFFSET GREATEST(v_page - 1, 0) * v_page_size LIMIT v_page_size;
END;
$$ LANGUAGE plpgsql;


-- ============================================================
-- 9) Giới hạn quyền — app chỉ EXECUTE function, không thao tác trực tiếp trên bảng
-- (thay <APP_DB_USER> bằng user Postgres thật app đang connect, bỏ comment khi chạy thật)
-- ============================================================
-- REVOKE ALL ON users, sessions, clients, login_history FROM <APP_DB_USER>;
-- GRANT EXECUTE ON FUNCTION
--     sp_user_create, sp_user_get_by_id, sp_user_get_by_username, sp_user_get_by_email,
--     sp_user_get_by_google_id, sp_user_get_paged, sp_user_update_password,
--     sp_user_update_totp_secret, sp_user_link_google, sp_user_update_role, sp_user_set_active,
--     sp_user_set_permanently_locked, sp_user_set_failed_otp_count, sp_user_soft_delete,
--     sp_session_create, sp_session_get_by_id, sp_session_find_by_refresh_hash,
--     sp_session_rotate_refresh_token, sp_session_get_active_by_user, sp_session_get_all_active,
--     sp_session_revoke, sp_session_revoke_all_by_user,
--     sp_client_create, sp_client_get_by_client_id, sp_client_get_all,
--     sp_client_regenerate_secret, sp_client_set_active,
--     sp_login_history_insert, sp_login_history_get_by_user
-- TO <APP_DB_USER>;
