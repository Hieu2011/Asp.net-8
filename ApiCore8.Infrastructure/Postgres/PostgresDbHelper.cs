using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using ApiCore8.Application.Abstractions;
using ApiCore8.Infrastructure.Database;

namespace ApiCore8.Infrastructure.Postgres;

public class PostgresDbHelper : IDisposable, IDataCore
{
    private readonly string _connectionString;
    private IDbConnection _connection;
    private IDbTransaction _transaction;
    private IDbCommand _command;
    internal List<NpgsqlParameter> _currentParameters = new();

    // Implement IDataCore interface
    IDbCommand IDataCore.ICommand
    {
        get { return _command; }
        set { _command = value; }
    }

    IDbTransaction IDataCore.ITransaction
    {
        get { return _transaction; }
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
        if (_connection == null)
        {
            _connection = new NpgsqlConnection(_connectionString);
        }

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

    // Thêm tham số
    public void AddParameter(string paramName, object value)
    {
        var param = new NpgsqlParameter(paramName, value ?? DBNull.Value);

        // Xử lý kiểu dữ liệu cụ thể
        if (value is Guid)
        {
            param.NpgsqlDbType = NpgsqlDbType.Uuid;
        }
        else if (value is DateTime)
        {
            // TimestampTz (timestamptz), không phải Timestamp (timestamp without time zone) —
            // khớp convention lưu UTC/timestamptz đã thống nhất cho toàn dự án.
            param.NpgsqlDbType = NpgsqlDbType.TimestampTz;
        }
        else if (value is bool)
        {
            param.NpgsqlDbType = NpgsqlDbType.Boolean;
        }
        else if (value is int[])
        {
            param.NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Integer;
        }

        _currentParameters.Add(param);
    }

    // Build câu gọi function bằng named notation ("tên_tham_số => giá_trị") — tách riêng để test
    // được độc lập, và dùng chung cho cả 3 nơi (ExecuteStoreDataTableAsync/ExecuteNonQueryAsync/
    // ExecuteNonQueryAsStringAsync) thay vì lặp lại 3 lần. Named notation không phụ thuộc thứ tự
    // tham số khai báo trong function Postgres — v_out (hay bất kỳ tham số nào) nằm ở vị trí nào
    // trong _currentParameters cũng ra cùng 1 kết quả, vì PostgreSQL tự khớp theo tên.
    internal static string BuildCallSql(string storeName, IReadOnlyList<NpgsqlParameter> parameters)
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

            using (_command = new NpgsqlCommand(sql, (NpgsqlConnection)_connection, (NpgsqlTransaction)_transaction))
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

            using (var fetchCmd = new NpgsqlCommand($"FETCH ALL IN \"{cursorName}\";", (NpgsqlConnection)_connection, (NpgsqlTransaction)_transaction))
            using (var fetchReader = await fetchCmd.ExecuteReaderAsync(token))
            {
                dt.Load(fetchReader);
            }

            try
            {
                using var closeCmd = new NpgsqlCommand($"CLOSE \"{cursorName}\";", (NpgsqlConnection)_connection, (NpgsqlTransaction)_transaction);
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

    // Thực thi stored procedure không trả về dữ liệu
    public async Task<int> ExecuteNonQueryAsync(string storeName, CancellationToken cancellationToken = default)
    {
        using var timeoutCts = CancellationTokenTimeoutHelper.CreateLinkedTimeoutSource(cancellationToken);
        var token = timeoutCts.Token;

        await EnsureOpenConnectionAsync(token);

        string sql = BuildCallSql(storeName, _currentParameters);

        using (_command = new NpgsqlCommand(sql, (NpgsqlConnection)_connection, (NpgsqlTransaction)_transaction))
        {
            if (_currentParameters.Any())
            {
                ((NpgsqlCommand)_command).Parameters.AddRange(_currentParameters.ToArray());
            }

            int result = await ((NpgsqlCommand)_command).ExecuteNonQueryAsync(token);
            ClearParameters();
            return result;
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
            using (_command = new NpgsqlCommand(sql, (NpgsqlConnection)_connection, (NpgsqlTransaction)_transaction))
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
        _connection = null;
    }
}