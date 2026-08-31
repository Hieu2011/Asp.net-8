-- ============================================================
-- SQL Server — bảng "users" + 5 stored procedure tương đương bản Postgres.
-- Không có khái niệm refcursor — SP trả result set thẳng qua SELECT, đơn giản nhất trong 3 provider.
-- update/delete trả 1/0 qua SELECT (đọc lại bằng ExecuteScalar ở SqlServerDbHelper), không dùng
-- RETURN (RETURN trong T-SQL chỉ cho phép trả int mã lỗi, không đọc được qua ExecuteScalar).
-- ============================================================

CREATE TABLE users (
    id            UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,
    username      NVARCHAR(100) NOT NULL UNIQUE,
    password_hash NVARCHAR(255) NOT NULL,
    full_name     NVARCHAR(200),
    email         NVARCHAR(255),
    is_active     BIT DEFAULT 1 NOT NULL,
    created_at    DATETIME2 DEFAULT SYSUTCDATETIME() NOT NULL,
    updated_at    DATETIME2 DEFAULT SYSUTCDATETIME() NOT NULL
);
GO

CREATE OR ALTER PROCEDURE sp_user_create
    @p_username      NVARCHAR(100),
    @p_password_hash NVARCHAR(255),
    @p_full_name     NVARCHAR(200),
    @p_email         NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @new_id UNIQUEIDENTIFIER = NEWID();

    INSERT INTO users (id, username, password_hash, full_name, email, is_active, created_at, updated_at)
    VALUES (@new_id, @p_username, @p_password_hash, @p_full_name, @p_email, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    SELECT id, username, password_hash, full_name, email, is_active, created_at, updated_at
    FROM users WHERE id = @new_id;
END
GO

CREATE OR ALTER PROCEDURE sp_user_get_by_id
    @p_id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT id, username, password_hash, full_name, email, is_active, created_at, updated_at
    FROM users WHERE id = @p_id;
END
GO

CREATE OR ALTER PROCEDURE sp_user_get_all
AS
BEGIN
    SET NOCOUNT ON;
    SELECT id, username, password_hash, full_name, email, is_active, created_at, updated_at
    FROM users ORDER BY created_at DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_user_update
    @p_id        UNIQUEIDENTIFIER,
    @p_full_name NVARCHAR(200),
    @p_email     NVARCHAR(255),
    @p_is_active BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE users
    SET full_name = @p_full_name, email = @p_email, is_active = @p_is_active, updated_at = SYSUTCDATETIME()
    WHERE id = @p_id;

    SELECT CASE WHEN @@ROWCOUNT > 0 THEN 1 ELSE 0 END;
END
GO

CREATE OR ALTER PROCEDURE sp_user_delete
    @p_id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM users WHERE id = @p_id;

    SELECT CASE WHEN @@ROWCOUNT > 0 THEN 1 ELSE 0 END;
END
GO

-- @p_from_date/@p_to_date: DATETIME2 — C# luôn truyền UTC (đã convert từ DateTimeOffset ở Controller).
CREATE OR ALTER PROCEDURE sp_user_search_by_date
    @p_from_date DATETIME2,
    @p_to_date   DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT id, username, password_hash, full_name, email, is_active, created_at, updated_at
    FROM users
    WHERE created_at BETWEEN @p_from_date AND @p_to_date
    ORDER BY created_at DESC;
END
GO

-- Giới hạn quyền: app chỉ EXECUTE stored procedure, không SELECT/INSERT/UPDATE/DELETE trực tiếp
-- trên bảng (thay <APP_DB_USER> bằng user SQL Server thật app đang connect)
-- DENY SELECT, INSERT, UPDATE, DELETE ON users TO <APP_DB_USER>;
-- GRANT EXECUTE ON sp_user_create TO <APP_DB_USER>;
-- GRANT EXECUTE ON sp_user_get_by_id TO <APP_DB_USER>;
-- GRANT EXECUTE ON sp_user_get_all TO <APP_DB_USER>;
-- GRANT EXECUTE ON sp_user_update TO <APP_DB_USER>;
-- GRANT EXECUTE ON sp_user_delete TO <APP_DB_USER>;
-- GRANT EXECUTE ON sp_user_search_by_date TO <APP_DB_USER>;
