-- ============================================================
-- Oracle — bảng "users" + 5 procedure tương đương bản Postgres
-- Id lưu VARCHAR2(32) dạng hex-32-ký-tự-không-gạch (Guid.Parse ở .NET đọc được cả 2 dạng
-- "N" và "D") vì Oracle không có kiểu UUID native.
-- Oracle không cho function/procedure trả PL/SQL BOOLEAN ra ngoài .NET được -> update/delete
-- dùng tham số OUT NUMBER (1/0) thay vì boolean.
-- ============================================================

CREATE TABLE users (
    id            VARCHAR2(32) DEFAULT LOWER(RAWTOHEX(SYS_GUID())) PRIMARY KEY,
    username      VARCHAR2(100) NOT NULL UNIQUE,
    password_hash VARCHAR2(255) NOT NULL,
    full_name     VARCHAR2(200),
    email         VARCHAR2(255),
    is_active     NUMBER(1) DEFAULT 1 NOT NULL,
    created_at    TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL,
    updated_at    TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL
);

CREATE OR REPLACE PROCEDURE sp_user_create (
    v_out          OUT SYS_REFCURSOR,
    p_username     IN VARCHAR2,
    p_password_hash IN VARCHAR2,
    p_full_name    IN VARCHAR2,
    p_email        IN VARCHAR2
) AS
    v_id VARCHAR2(32);
BEGIN
    INSERT INTO users (username, password_hash, full_name, email, is_active, created_at, updated_at)
    VALUES (p_username, p_password_hash, p_full_name, p_email, 1, SYSTIMESTAMP, SYSTIMESTAMP)
    RETURNING id INTO v_id;

    OPEN v_out FOR
        SELECT id, username, password_hash, full_name, email, is_active, created_at, updated_at
        FROM users WHERE id = v_id;
END sp_user_create;
/

CREATE OR REPLACE PROCEDURE sp_user_get_by_id (
    v_out OUT SYS_REFCURSOR,
    p_id  IN VARCHAR2
) AS
BEGIN
    OPEN v_out FOR
        SELECT id, username, password_hash, full_name, email, is_active, created_at, updated_at
        FROM users WHERE id = p_id;
END sp_user_get_by_id;
/

CREATE OR REPLACE PROCEDURE sp_user_get_all (
    v_out OUT SYS_REFCURSOR
) AS
BEGIN
    OPEN v_out FOR
        SELECT id, username, password_hash, full_name, email, is_active, created_at, updated_at
        FROM users ORDER BY created_at DESC;
END sp_user_get_all;
/

CREATE OR REPLACE PROCEDURE sp_user_update (
    v_out         OUT NUMBER,
    p_id          IN VARCHAR2,
    p_full_name   IN VARCHAR2,
    p_email       IN VARCHAR2,
    p_is_active   IN NUMBER
) AS
BEGIN
    UPDATE users
    SET full_name = p_full_name, email = p_email, is_active = p_is_active, updated_at = SYSTIMESTAMP
    WHERE id = p_id;

    v_out := CASE WHEN SQL%ROWCOUNT > 0 THEN 1 ELSE 0 END;
END sp_user_update;
/

CREATE OR REPLACE PROCEDURE sp_user_delete (
    v_out OUT NUMBER,
    p_id  IN VARCHAR2
) AS
BEGIN
    DELETE FROM users WHERE id = p_id;
    v_out := CASE WHEN SQL%ROWCOUNT > 0 THEN 1 ELSE 0 END;
END sp_user_delete;
/

-- p_from_date/p_to_date: TIMESTAMP — C# luôn truyền UTC (đã convert từ DateTimeOffset ở Controller).
CREATE OR REPLACE PROCEDURE sp_user_search_by_date (
    v_out        OUT SYS_REFCURSOR,
    p_from_date  IN TIMESTAMP,
    p_to_date    IN TIMESTAMP
) AS
BEGIN
    OPEN v_out FOR
        SELECT id, username, password_hash, full_name, email, is_active, created_at, updated_at
        FROM users
        WHERE created_at BETWEEN p_from_date AND p_to_date
        ORDER BY created_at DESC;
END sp_user_search_by_date;
/

-- Giới hạn quyền: app chỉ EXECUTE procedure, không SELECT/INSERT/UPDATE/DELETE trực tiếp trên bảng
-- (thay <APP_DB_USER> bằng user Oracle thật app đang connect)
-- REVOKE ALL ON users FROM <APP_DB_USER>;
-- GRANT EXECUTE ON sp_user_create TO <APP_DB_USER>;
-- GRANT EXECUTE ON sp_user_get_by_id TO <APP_DB_USER>;
-- GRANT EXECUTE ON sp_user_get_all TO <APP_DB_USER>;
-- GRANT EXECUTE ON sp_user_update TO <APP_DB_USER>;
-- GRANT EXECUTE ON sp_user_delete TO <APP_DB_USER>;
-- GRANT EXECUTE ON sp_user_search_by_date TO <APP_DB_USER>;
