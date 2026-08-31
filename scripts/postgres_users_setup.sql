-- ============================================================
-- Postgres — bảng "users" + 6 function tương đương bản Oracle/SQL Server.
-- v_out REFCURSOR — driver Npgsql cần đọc qua OPEN/FETCH/CLOSE trong 1 transaction
-- (PostgresDbHelper.ExecuteStoreDataTableAsync tự mở transaction cục bộ nếu chưa có).
-- ============================================================

CREATE TABLE users (
    id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    username      VARCHAR(100) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    full_name     VARCHAR(200),
    email         VARCHAR(255),
    is_active     BOOLEAN NOT NULL DEFAULT true,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at    TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE OR REPLACE FUNCTION sp_user_create(
    INOUT v_out refcursor,
    p_username varchar,
    p_password_hash varchar,
    p_full_name varchar,
    p_email varchar
) AS $$
BEGIN
    OPEN v_out FOR
    INSERT INTO users (username, password_hash, full_name, email, is_active, created_at, updated_at)
    VALUES (p_username, p_password_hash, p_full_name, p_email, true, now(), now())
    RETURNING id, username, password_hash, full_name, email, is_active, created_at, updated_at;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION sp_user_get_by_id(INOUT v_out refcursor, p_id uuid) AS $$
BEGIN
    OPEN v_out FOR
    SELECT id, username, password_hash, full_name, email, is_active, created_at, updated_at
    FROM users WHERE id = p_id;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION sp_user_get_all(INOUT v_out refcursor) AS $$
BEGIN
    OPEN v_out FOR
    SELECT id, username, password_hash, full_name, email, is_active, created_at, updated_at
    FROM users ORDER BY created_at DESC;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION sp_user_update(
    p_id uuid, p_full_name varchar, p_email varchar, p_is_active boolean
) RETURNS boolean AS $$
DECLARE affected int;
BEGIN
    UPDATE users SET full_name = p_full_name, email = p_email, is_active = p_is_active, updated_at = now()
    WHERE id = p_id;
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected > 0;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION sp_user_delete(p_id uuid) RETURNS boolean AS $$
DECLARE affected int;
BEGIN
    DELETE FROM users WHERE id = p_id;
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected > 0;
END;
$$ LANGUAGE plpgsql;

-- p_from_date/p_to_date: timestamptz — C# luôn truyền UTC (đã convert từ DateTimeOffset ở Controller).
CREATE OR REPLACE FUNCTION sp_user_search_by_date(
    INOUT v_out refcursor,
    p_from_date timestamptz,
    p_to_date timestamptz
) AS $$
BEGIN
    OPEN v_out FOR
    SELECT id, username, password_hash, full_name, email, is_active, created_at, updated_at
    FROM users
    WHERE created_at BETWEEN p_from_date AND p_to_date
    ORDER BY created_at DESC;
END;
$$ LANGUAGE plpgsql;

-- Giới hạn quyền: app chỉ EXECUTE function, không SELECT/INSERT/UPDATE/DELETE trực tiếp trên bảng
-- (thay <APP_DB_USER> bằng user Postgres thật app đang connect)
-- REVOKE ALL ON users FROM <APP_DB_USER>;
-- GRANT EXECUTE ON FUNCTION sp_user_create, sp_user_get_by_id, sp_user_get_all, sp_user_update,
--     sp_user_delete, sp_user_search_by_date TO <APP_DB_USER>;
