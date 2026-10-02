using System.Data;
using System.Text;
using Npgsql;
using NpgsqlTypes;
using Shared.DataAccess.Abstractions;
using Shared.DataAccess.Database;

namespace Shared.DataAccess.Postgres;

public class PostgresDbHelper : IDisposable, IDataCore
{
    private readonly string _connectionString;
    private IDbConnection _connection;
    private IDbTransaction? _transaction;
    private IDbCommand? _command;
    private readonly List<NpgsqlParameter> _currentParameters = new();

    // Implement IDataCore interface
    IDbCommand IDataCore.ICommand
    {
        get { return _command!; }
        set { _command = value; }
    }

    IDbTransaction IDataCore.ITransaction
    {
        get { return _transaction!; }
        set { _transaction = value; }
    }

    IDbConnection IDataCore.IConnection
    {
        get { return _connection; }
        set { _connection = value; }
    }

    public PostgresDbHelper(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentException("Connection string is required.", nameof(connectionString));
        }

        _connectionString = connectionString;
        _connection = new NpgsqlConnection(_connectionString);
    }

    // Mở kết nối nếu chưa mở
    private async Task EnsureOpenConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection.State == ConnectionState.Closed || _connection.State == ConnectionState.Broken)
        {
            try
            {
                await ((NpgsqlConnection)_connection).OpenAsync(cancellationToken);
            }
            catch (NpgsqlException)
            {
                // Thử tạo kết nối mới nếu không mở được kết nối cũ
                _connection.Dispose();
                _connection = new NpgsqlConnection(_connectionString);
                await ((NpgsqlConnection)_connection).OpenAsync(cancellationToken);
            }
        }
    }

    // Bắt đầu transaction
    public async Task StartTransactionScopeAsync(CancellationToken cancellationToken = default)
    {
        await EnsureOpenConnectionAsync(cancellationToken);
        _transaction = await ((NpgsqlConnection)_connection).BeginTransactionAsync(cancellationToken);
    }

    // Commit transaction
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await ((NpgsqlTransaction)_transaction).CommitAsync(cancellationToken);
            _transaction.Dispose();
            _transaction = null;
        }
    }

    // Rollback transaction
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await ((NpgsqlTransaction)_transaction).RollbackAsync(cancellationToken);
            _transaction.Dispose();
            _transaction = null;
        }
    }

    // Thêm tham số — paramName truyền vào dạng "@ten_gon" (VD "@from_date") sẽ tự bỏ dấu "@"
    // và thêm tiền tố "v_" thành tên tham số IN thật khớp convention SQL ("v_from_date").
    // Không có dấu "@" thì giữ nguyên chuỗi truyền vào (tương thích ngược, không bắt buộc sửa
    // toàn bộ call site cùng lúc).
    public void AddParameter(string paramName, object? value)
    {
        string pgParamName = paramName.StartsWith('@') ? "v_" + paramName[1..] : paramName;

        // Enum (VD RoleLevel) → SMALLINT thật (giá trị số), Npgsql không tự biết ghi enum .NET.
        var actualValue = value is Enum enumValue ? Convert.ToInt16(enumValue) : value;

        var param = new NpgsqlParameter(pgParamName, actualValue ?? DBNull.Value);

        // Xử lý kiểu dữ liệu cụ thể
        if (actualValue is Guid)
        {
            param.NpgsqlDbType = NpgsqlDbType.Uuid;
        }
        else if (actualValue is DateTime)
        {
            // TimestampTz (timestamptz), không phải Timestamp (timestamp without time zone) —
            // khớp convention lưu UTC/timestamptz đã thống nhất cho toàn dự án.
            param.NpgsqlDbType = NpgsqlDbType.TimestampTz;
        }
        else if (actualValue is bool)
        {
            param.NpgsqlDbType = NpgsqlDbType.Boolean;
        }
        else if (value is Enum || actualValue is short)
        {
            param.NpgsqlDbType = NpgsqlDbType.Smallint;
        }
        else if (actualValue is int[])
        {
            param.NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Integer;
        }
        else if (actualValue is IEnumerable<string> && actualValue is not string)
        {
            param.NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Text;
        }
        else if (actualValue is null)
        {
            // Null "trần" (VD lọc role_level = NULL nghĩa là không lọc, hoặc google_id chưa liên
            // kết) — không có type info gì để suy luận, để Postgres tự infer kiểu theo signature
            // function thay vì Npgsql đoán sai. LƯU Ý: check "actualValue is null", KHÔNG PHẢI
            // "actualValue is DBNull" — caller luôn truyền C# null (VD RoleLevel? = null), giá trị
            // đó chỉ được đổi thành DBNull.Value bên trong constructor NpgsqlParameter ở trên, biến
            // actualValue vẫn giữ nguyên null. Check "is DBNull" ở đây sẽ luôn false — nhánh chết,
            // khiến null bị gửi đi KHÔNG có NpgsqlDbType tường minh (bug thật, đã bắt được khi review).
            param.NpgsqlDbType = NpgsqlDbType.Unknown;
        }

        _currentParameters.Add(param);
    }

    // Build câu gọi function bằng named notation ("tên_tham_số => giá_trị") — tách riêng để test
    // được độc lập, và dùng chung cho cả 3 nơi (ExecuteStoreDataTableAsync/ExecuteNonQueryAsync/
    // ExecuteNonQueryAsStringAsync) thay vì lặp lại 3 lần. Named notation không phụ thuộc thứ tự
    // tham số khai báo trong function Postgres — v_out (hay bất kỳ tham số nào) nằm ở vị trí nào
    // trong _currentParameters cũng ra cùng 1 kết quả, vì PostgreSQL tự khớp theo tên.
    private static string BuildCallSql(string storeName, IReadOnlyList<NpgsqlParameter> parameters)
    {
        var sqlBuilder = new StringBuilder();
        sqlBuilder.Append("SELECT ");
        sqlBuilder.Append(storeName);
        sqlBuilder.Append('(');
        sqlBuilder.Append(string.Join(", ", parameters.Select(p => $"{p.ParameterName} => @{p.ParameterName}")));
        sqlBuilder.Append(");");
        return sqlBuilder.ToString();
    }

    // Xóa tham số
    public void ClearParameters()
    {
        _currentParameters.Clear();
    }

    // Thực thi stored procedure trả về DataTable (refcursor)
    public async Task<DataTable> ExecuteStoreDataTableAsync(string storeName, CancellationToken cancellationToken = default)
    {
        // Đảm bảo có giới hạn thời gian tối thiểu (DefaultTimeout) dù caller không truyền
        // CancellationToken nào — CancellationToken.None mặc định không bao giờ tự hủy.
        using var timeoutCts = CancellationTokenTimeoutHelper.CreateLinkedTimeoutSource(cancellationToken);
        var token = timeoutCts.Token;

        await EnsureOpenConnectionAsync(token);
        DataTable dt = new DataTable();
        string cursorName = storeName;

        // Refcursor (OPEN/FETCH/CLOSE) chỉ tồn tại trong phạm vi 1 transaction — ở chế độ
        // autocommit mặc định, statement mở cursor tự commit xong là cursor bị đóng ngay,
        // FETCH ở lệnh sau sẽ báo "cursor does not exist". Nếu caller chưa chủ động mở
        // transaction (StartTransactionScopeAsync cho nghiệp vụ nhiều bước), tự mở 1
        // transaction cục bộ bao trọn cả 3 lệnh rồi tự commit/rollback.
        bool ownTransaction = _transaction == null;
        if (ownTransaction)
        {
            await StartTransactionScopeAsync(token);
        }

        try
        {
            // Tự động thêm tham số v_out (REFCURSOR) vào đầu danh sách nếu chưa có
            if (!_currentParameters.Any(p => p.NpgsqlDbType == NpgsqlDbType.Refcursor))
            {
                _currentParameters.Insert(0, new NpgsqlParameter
                {
                    ParameterName = "v_out",
                    NpgsqlDbType = NpgsqlDbType.Refcursor,
                    Value = DBNull.Value
                });
            }

            string sql = BuildCallSql(storeName, _currentParameters);

            using (_command = new NpgsqlCommand(sql, (NpgsqlConnection)_connection, (NpgsqlTransaction?)_transaction))
            {
                ((NpgsqlCommand)_command).Parameters.AddRange(_currentParameters.ToArray());

                using (var reader = await ((NpgsqlCommand)_command).ExecuteReaderAsync(token))
                {
                    if (!await reader.ReadAsync(token) || reader.IsDBNull(0))
                    {
                        if (ownTransaction) await CommitTransactionAsync(token);
                        return dt;
                    }
                    cursorName = reader.GetString(0);
                }
            }

            using (var fetchCmd = new NpgsqlCommand($"FETCH ALL IN \"{cursorName}\";", (NpgsqlConnection)_connection, (NpgsqlTransaction?)_transaction))
            using (var fetchReader = await fetchCmd.ExecuteReaderAsync(token))
            {
                dt.Load(fetchReader);
            }

            try
            {
                using var closeCmd = new NpgsqlCommand($"CLOSE \"{cursorName}\";", (NpgsqlConnection)_connection, (NpgsqlTransaction?)_transaction);
                await closeCmd.ExecuteNonQueryAsync(token);
            }
            catch { /* refcursor có thể đã tự đóng khi transaction commit — bỏ qua */ }

            if (ownTransaction)
            {
                await CommitTransactionAsync(token);
            }
        }
        catch (Exception ex)
        {
            if (ownTransaction)
            {
                await RollbackTransactionAsync(token);
            }
            Console.WriteLine($"Error executing store {storeName}: {ex.Message}");
            throw;
        }
        finally
        {
            ClearParameters();
        }
        return dt;
    }

    // Thực thi stored procedure trả về object
    public async Task<T> ExecStoreToObjectAsync<T>(string storeName, CancellationToken cancellationToken = default)
    {
        var dataTable = await ExecuteStoreDataTableAsync(storeName, cancellationToken);
        return dataTable?.Rows.Count > 0
            ? DataRowMapper.GetItem<T>(dataTable.Rows[0])
            : Activator.CreateInstance<T>();
    }

    // Thực thi stored procedure trả về list object
    public async Task<List<T>> ExecStoreToListObjectAsync<T>(string storeName, CancellationToken cancellationToken = default)
    {
        var dataTable = await ExecuteStoreDataTableAsync(storeName, cancellationToken);
        return dataTable?.Rows.Count > 0
            ? DataRowMapper.ConvertDataTableToList<T>(dataTable)
            : new List<T>();
    }

    // Bản "fast" của ExecStoreToListObjectAsync — đọc thẳng qua FETCH refcursor bằng
    // CompiledReaderMapper, KHÔNG dựng DataTable trung gian. Vẫn tự động map (không cần viết tay),
    // vẫn giữ nguyên cơ chế transaction cục bộ cho refcursor — chỉ khác bước đọc kết quả cuối cùng.
    public async Task<List<T>> ExecStoreToListObjectFastAsync<T>(string storeName, CancellationToken cancellationToken = default)
    {
        using var timeoutCts = CancellationTokenTimeoutHelper.CreateLinkedTimeoutSource(cancellationToken);
        var token = timeoutCts.Token;

        await EnsureOpenConnectionAsync(token);
        var list = new List<T>();
        string cursorName = storeName;

        bool ownTransaction = _transaction == null;
        if (ownTransaction)
        {
            await StartTransactionScopeAsync(token);
        }

        try
        {
            if (!_currentParameters.Any(p => p.NpgsqlDbType == NpgsqlDbType.Refcursor))
            {
                _currentParameters.Insert(0, new NpgsqlParameter
                {
                    ParameterName = "v_out",
                    NpgsqlDbType = NpgsqlDbType.Refcursor,
                    Value = DBNull.Value
                });
            }

            string sql = BuildCallSql(storeName, _currentParameters);

            using (_command = new NpgsqlCommand(sql, (NpgsqlConnection)_connection, (NpgsqlTransaction?)_transaction))
            {
                ((NpgsqlCommand)_command).Parameters.AddRange(_currentParameters.ToArray());

                using (var reader = await ((NpgsqlCommand)_command).ExecuteReaderAsync(token))
                {
                    if (!await reader.ReadAsync(token) || reader.IsDBNull(0))
                    {
                        if (ownTransaction) await CommitTransactionAsync(token);
                        return list;
                    }
                    cursorName = reader.GetString(0);
                }
            }

            using (var fetchCmd = new NpgsqlCommand($"FETCH ALL IN \"{cursorName}\";", (NpgsqlConnection)_connection, (NpgsqlTransaction?)_transaction))
            using (var fetchReader = await fetchCmd.ExecuteReaderAsync(token))
            {
                // Build + compile mapper 1 lần cho lần gọi này (dựa theo cột thật của reader) —
                // không phải reflection mỗi dòng như DataRowMapper.
                var mapper = CompiledReaderMapper.Build<T>(fetchReader);
                while (await fetchReader.ReadAsync(token))
                {
                    list.Add(mapper(fetchReader));
                }
            }

            try
            {
                using var closeCmd = new NpgsqlCommand($"CLOSE \"{cursorName}\";", (NpgsqlConnection)_connection, (NpgsqlTransaction?)_transaction);
                await closeCmd.ExecuteNonQueryAsync(token);
            }
            catch { }

            if (ownTransaction)
            {
                await CommitTransactionAsync(token);
            }
        }
        catch (Exception ex)
        {
            if (ownTransaction)
            {
                await RollbackTransactionAsync(token);
            }
            Console.WriteLine($"Error executing store {storeName}: {ex.Message}");
            throw;
        }
        finally
        {
            ClearParameters();
        }
        return list;
    }

    // Bản "fast" của ExecStoreToObjectAsync — đọc thẳng qua FETCH refcursor bằng CompiledReaderMapper,
    // KHÔNG dựng DataTable trung gian. Chỉ cần 1 dòng nên FETCH 1 thay vì FETCH ALL (khác
    // ExecStoreToListObjectFastAsync) — Postgres không phải kéo hết result set về chỉ để lấy dòng đầu.
    public async Task<T> ExecStoreObjectFastAsync<T>(string storeName, CancellationToken cancellationToken = default)
    {
        using var timeoutCts = CancellationTokenTimeoutHelper.CreateLinkedTimeoutSource(cancellationToken);
        var token = timeoutCts.Token;

        await EnsureOpenConnectionAsync(token);
        string cursorName = storeName;
        T result;

        bool ownTransaction = _transaction == null;
        if (ownTransaction)
        {
            await StartTransactionScopeAsync(token);
        }

        try
        {
            if (!_currentParameters.Any(p => p.NpgsqlDbType == NpgsqlDbType.Refcursor))
            {
                _currentParameters.Insert(0, new NpgsqlParameter
                {
                    ParameterName = "v_out",
                    NpgsqlDbType = NpgsqlDbType.Refcursor,
                    Value = DBNull.Value
                });
            }

            string sql = BuildCallSql(storeName, _currentParameters);

            using (_command = new NpgsqlCommand(sql, (NpgsqlConnection)_connection, (NpgsqlTransaction?)_transaction))
            {
                ((NpgsqlCommand)_command).Parameters.AddRange(_currentParameters.ToArray());

                using (var reader = await ((NpgsqlCommand)_command).ExecuteReaderAsync(token))
                {
                    if (!await reader.ReadAsync(token) || reader.IsDBNull(0))
                    {
                        if (ownTransaction) await CommitTransactionAsync(token);
                        return Activator.CreateInstance<T>();
                    }
                    cursorName = reader.GetString(0);
                }
            }

            using (var fetchCmd = new NpgsqlCommand($"FETCH 1 IN \"{cursorName}\";", (NpgsqlConnection)_connection, (NpgsqlTransaction?)_transaction))
            using (var fetchReader = await fetchCmd.ExecuteReaderAsync(token))
            {
                var mapper = CompiledReaderMapper.Build<T>(fetchReader);
                result = await fetchReader.ReadAsync(token) ? mapper(fetchReader) : Activator.CreateInstance<T>();
            }

            try
            {
                using var closeCmd = new NpgsqlCommand($"CLOSE \"{cursorName}\";", (NpgsqlConnection)_connection, (NpgsqlTransaction?)_transaction);
                await closeCmd.ExecuteNonQueryAsync(token);
            }
            catch { }

            if (ownTransaction)
            {
                await CommitTransactionAsync(token);
            }
        }
        catch (Exception ex)
        {
            if (ownTransaction)
            {
                await RollbackTransactionAsync(token);
            }
            Console.WriteLine($"Error executing store {storeName}: {ex.Message}");
            throw;
        }
        finally
        {
            ClearParameters();
        }
        return result;
    }

    // Thực thi stored procedure không trả về dữ liệu
    public async Task<int> ExecuteNonQueryAsync(string storeName, CancellationToken cancellationToken = default)
    {
        using var timeoutCts = CancellationTokenTimeoutHelper.CreateLinkedTimeoutSource(cancellationToken);
        var token = timeoutCts.Token;

        await EnsureOpenConnectionAsync(token);

        string sql = BuildCallSql(storeName, _currentParameters);

        try
        {
            using (_command = new NpgsqlCommand(sql, (NpgsqlConnection)_connection, (NpgsqlTransaction?)_transaction))
            {
                if (_currentParameters.Any())
                {
                    ((NpgsqlCommand)_command).Parameters.AddRange(_currentParameters.ToArray());
                }

                return await ((NpgsqlCommand)_command).ExecuteNonQueryAsync(token);
            }
        }
        finally
        {
            // Trước đây thiếu try/finally — nếu ExecuteNonQueryAsync throw, ClearParameters() không
            // được gọi, tham số cũ còn sót lại cho lần gọi kế tiếp trên cùng instance (bug thật, phát
            // hiện khi review lại toàn bộ PostgresDbHelper — khác ExecuteNonQueryAsStringAsync và các
            // hàm refcursor vốn đã có finally đầy đủ).
            ClearParameters();
        }
    }
    // Thực thi stored procedure trả về chuỗi kết quả
    public async Task<string> ExecuteNonQueryAsStringAsync(string storeName, CancellationToken cancellationToken = default)
    {
        using var timeoutCts = CancellationTokenTimeoutHelper.CreateLinkedTimeoutSource(cancellationToken);
        var token = timeoutCts.Token;

        await EnsureOpenConnectionAsync(token);

        string sql = BuildCallSql(storeName, _currentParameters);

        try
        {
            using (_command = new NpgsqlCommand(sql, (NpgsqlConnection)_connection, (NpgsqlTransaction?)_transaction))
            {
                if (_currentParameters.Any())
                {
                    ((NpgsqlCommand)_command).Parameters.AddRange(_currentParameters.ToArray());
                }

                // Thực thi và đọc kết quả
                object result = await ((NpgsqlCommand)_command).ExecuteScalarAsync(token);
                return result?.ToString() ?? string.Empty;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing store {storeName}: {ex.Message}");
            throw;
        }
        finally
        {
            ClearParameters();
        }
    }

    // Giải phóng tài nguyên
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _transaction?.Dispose();
            _command?.Dispose();
            if (_connection != null)
            {
                if (_connection.State != ConnectionState.Closed)
                {
                    _connection.Close();
                }
                _connection.Dispose();
            }
        }
        _transaction = null;
        _command = null;
        _connection = null!;
    }
}
