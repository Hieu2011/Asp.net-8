using System.Data;
using ApiCore8.Application.Abstractions;
using ApiCore8.Infrastructure.Database;
using Oracle.ManagedDataAccess.Client;

namespace ApiCore8.Infrastructure.Oracle;

/// <summary>
/// Implement IDataCore cho Oracle (Oracle.ManagedDataAccess.Core) — gọi stored procedure qua
/// CommandType.StoredProcedure chuẩn ADO.NET, tham số OUT kiểu RefCursor. Khác Postgres: Oracle
/// client tự "giải mã" OUT REF CURSOR thành DataReader ngay trong 1 round-trip — không cần tự
/// FETCH/CLOSE tay, không cần transaction cục bộ bao quanh (giới hạn kỹ thuật của Postgres,
/// không áp dụng cho Oracle).
/// </summary>
public class OracleDbHelper : IDisposable, IDataCore
{
    private const string DefaultOutParameterName = "v_out";

    private readonly string _connectionString;
    private IDbConnection _connection;
    private IDbTransaction _transaction;
    private IDbCommand _command;
    private List<OracleParameter> _currentParameters = new();

    IDbCommand IDataCore.ICommand
    {
        get => _command;
        set => _command = value;
    }

    IDbTransaction IDataCore.ITransaction
    {
        get => _transaction;
        set => _transaction = value;
    }

    IDbConnection IDataCore.IConnection
    {
        get => _connection;
        set => _connection = value;
    }

    public OracleDbHelper(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentException("Connection string is required.", nameof(connectionString));
        }

        _connectionString = connectionString;
        _connection = new OracleConnection(_connectionString);
    }

    private async Task EnsureOpenConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection == null)
        {
            _connection = new OracleConnection(_connectionString);
        }

        if (_connection.State == ConnectionState.Closed || _connection.State == ConnectionState.Broken)
        {
            try
            {
                await ((OracleConnection)_connection).OpenAsync(cancellationToken);
            }
            catch (OracleException)
            {
                _connection.Dispose();
                _connection = new OracleConnection(_connectionString);
                await ((OracleConnection)_connection).OpenAsync(cancellationToken);
            }
        }
    }

    // BeginTransaction/Commit/Rollback của ODP.NET Core là API đồng bộ (không có bản Async) —
    // không có gì để truyền cancellationToken vào, chỉ dùng nó cho bước mở connection phía trên.
    public async Task StartTransactionScopeAsync(CancellationToken cancellationToken = default)
    {
        await EnsureOpenConnectionAsync(cancellationToken);
        _transaction = ((OracleConnection)_connection).BeginTransaction();
    }

    public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            ((OracleTransaction)_transaction).Commit();
            _transaction.Dispose();
            _transaction = null;
        }
        return Task.CompletedTask;
    }

    public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            ((OracleTransaction)_transaction).Rollback();
            _transaction.Dispose();
            _transaction = null;
        }
        return Task.CompletedTask;
    }

    public void AddParameter(string paramName, object value)
    {
        // Oracle không có kiểu boolean nguyên bản (trước 23c) — convert true/false thành 1/0
        // giống quy ước cũ (tránh lỗi kiểu khi function nhận NUMBER/PLS_INTEGER cho cờ boolean).
        if (value is bool boolValue)
        {
            value = boolValue ? 1 : 0;
        }

        var param = new OracleParameter(paramName, value ?? DBNull.Value);

        // new OracleParameter(name, value) mặc định tự suy OracleDbType.Date cho DateTime (chỉ có
        // ngày, không giờ/giây) — ép rõ TimeStamp để khớp cột TIMESTAMP trong DDL (giữ được giờ/giây).
        if (value is DateTime)
        {
            param.OracleDbType = OracleDbType.TimeStamp;
        }

        _currentParameters.Add(param);
    }

    public void ClearParameters()
    {
        _currentParameters.Clear();
    }

    private OracleCommand CreateStoredProcedureCommand(string storeName)
    {
        var cmd = new OracleCommand(storeName, (OracleConnection)_connection)
        {
            CommandType = CommandType.StoredProcedure,
            BindByName = true
        };

        if (_transaction != null)
        {
            cmd.Transaction = (OracleTransaction)_transaction;
        }

        cmd.Parameters.AddRange(_currentParameters.ToArray());
        return cmd;
    }

    public async Task<DataTable> ExecuteStoreDataTableAsync(string storeName, CancellationToken cancellationToken = default)
    {
        using var timeoutCts = CancellationTokenTimeoutHelper.CreateLinkedTimeoutSource(cancellationToken);
        var token = timeoutCts.Token;

        await EnsureOpenConnectionAsync(token);
        var dt = new DataTable();

        try
        {
            using (_command = CreateStoredProcedureCommand(storeName))
            {
                var oracleCommand = (OracleCommand)_command;
                oracleCommand.Parameters.Add(DefaultOutParameterName, OracleDbType.RefCursor).Direction = ParameterDirection.Output;

                using var reader = await oracleCommand.ExecuteReaderAsync(token);
                dt.Load(reader);
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

        return dt;
    }

    // Bản "fast" của ExecStoreToListObjectAsync — đọc thẳng OracleDataReader bằng
    // CompiledReaderMapper, KHÔNG dựng DataTable trung gian. Oracle đọc refcursor xong trong đúng
    // 1 round-trip (không cần transaction cục bộ như Postgres) nên đơn giản hơn hẳn.
    public async Task<List<T>> ExecStoreToListObjectFastAsync<T>(string storeName, CancellationToken cancellationToken = default)
    {
        using var timeoutCts = CancellationTokenTimeoutHelper.CreateLinkedTimeoutSource(cancellationToken);
        var token = timeoutCts.Token;

        await EnsureOpenConnectionAsync(token);
        var list = new List<T>();

        try
        {
            using (_command = CreateStoredProcedureCommand(storeName))
            {
                var oracleCommand = (OracleCommand)_command;
                oracleCommand.Parameters.Add(DefaultOutParameterName, OracleDbType.RefCursor).Direction = ParameterDirection.Output;

                using var reader = await oracleCommand.ExecuteReaderAsync(token);
                var mapper = CompiledReaderMapper.Build<T>(reader);
                while (await reader.ReadAsync(token))
                {
                    list.Add(mapper(reader));
                }
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

        return list;
    }

    public async Task<T> ExecStoreToObjectAsync<T>(string storeName, CancellationToken cancellationToken = default)
    {
        var dataTable = await ExecuteStoreDataTableAsync(storeName, cancellationToken);
        return dataTable.Rows.Count > 0
            ? DataRowMapper.GetItem<T>(dataTable.Rows[0])
            : Activator.CreateInstance<T>();
    }

    public async Task<List<T>> ExecStoreToListObjectAsync<T>(string storeName, CancellationToken cancellationToken = default)
    {
        var dataTable = await ExecuteStoreDataTableAsync(storeName, cancellationToken);
        return dataTable.Rows.Count > 0
            ? DataRowMapper.ConvertDataTableToList<T>(dataTable)
            : new List<T>();
    }

    public async Task<int> ExecuteNonQueryAsync(string storeName, CancellationToken cancellationToken = default)
    {
        using var timeoutCts = CancellationTokenTimeoutHelper.CreateLinkedTimeoutSource(cancellationToken);
        var token = timeoutCts.Token;

        await EnsureOpenConnectionAsync(token);

        using (_command = CreateStoredProcedureCommand(storeName))
        {
            int result = await ((OracleCommand)_command).ExecuteNonQueryAsync(token);
            ClearParameters();
            return result;
        }
    }

    // Oracle procedure không có "giá trị trả về" đọc qua ExecuteScalar như Postgres/SQL Server —
    // phải tự thêm tham số OUT kiểu NUMBER (giống hệt cách v_out REFCURSOR được tự thêm ở
    // ExecuteStoreDataTableAsync), rồi đọc lại giá trị của tham số đó sau khi ExecuteNonQuery.
    public async Task<string> ExecuteNonQueryAsStringAsync(string storeName, CancellationToken cancellationToken = default)
    {
        using var timeoutCts = CancellationTokenTimeoutHelper.CreateLinkedTimeoutSource(cancellationToken);
        var token = timeoutCts.Token;

        await EnsureOpenConnectionAsync(token);

        try
        {
            using (_command = CreateStoredProcedureCommand(storeName))
            {
                var oracleCommand = (OracleCommand)_command;
                var outParam = oracleCommand.Parameters.Add(DefaultOutParameterName, OracleDbType.Decimal);
                outParam.Direction = ParameterDirection.Output;

                await oracleCommand.ExecuteNonQueryAsync(token);
                return outParam.Value?.ToString() ?? string.Empty;
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
