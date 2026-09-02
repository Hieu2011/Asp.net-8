using System.Data;
using ApiCore8.Application.Abstractions;
using ApiCore8.Infrastructure.Database;
using Microsoft.Data.SqlClient;

namespace ApiCore8.Infrastructure.SqlServer;

/// <summary>
/// Implement IDataCore cho SQL Server (Microsoft.Data.SqlClient) — gọi stored procedure qua
/// CommandType.StoredProcedure chuẩn ADO.NET. SQL Server không có khái niệm refcursor — SP trả
/// thẳng result set qua SqlDataReader, đơn giản hơn cả Postgres lẫn Oracle (không cần OUT
/// parameter đặc biệt, không cần transaction cục bộ).
/// </summary>
public class SqlServerDbHelper : IDisposable, IDataCore
{
    private readonly string _connectionString;
    private IDbConnection _connection;
    private IDbTransaction _transaction;
    private IDbCommand _command;
    private List<SqlParameter> _currentParameters = new();

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

    public SqlServerDbHelper(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentException("Connection string is required.", nameof(connectionString));
        }

        _connectionString = connectionString;
        _connection = new SqlConnection(_connectionString);
    }

    private async Task EnsureOpenConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection == null)
        {
            _connection = new SqlConnection(_connectionString);
        }

        if (_connection.State == ConnectionState.Closed || _connection.State == ConnectionState.Broken)
        {
            try
            {
                await ((SqlConnection)_connection).OpenAsync(cancellationToken);
            }
            catch (SqlException)
            {
                _connection.Dispose();
                _connection = new SqlConnection(_connectionString);
                await ((SqlConnection)_connection).OpenAsync(cancellationToken);
            }
        }
    }

    public async Task StartTransactionScopeAsync(CancellationToken cancellationToken = default)
    {
        await EnsureOpenConnectionAsync(cancellationToken);
        _transaction = await ((SqlConnection)_connection).BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await ((SqlTransaction)_transaction).CommitAsync(cancellationToken);
            _transaction.Dispose();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await ((SqlTransaction)_transaction).RollbackAsync(cancellationToken);
            _transaction.Dispose();
            _transaction = null;
        }
    }

    public void AddParameter(string paramName, object value)
    {
        var name = paramName.StartsWith("@") ? paramName : "@" + paramName;
        var param = new SqlParameter(name, value ?? DBNull.Value);

        // SqlParameter(name, value) mặc định tự suy SqlDbType.DateTime cho DateTime (kiểu cũ, độ
        // chính xác ~3ms, phạm vi 1753-9999) — ép rõ DateTime2 để khớp cột DATETIME2 trong DDL.
        if (value is DateTime)
        {
            param.SqlDbType = SqlDbType.DateTime2;
        }

        _currentParameters.Add(param);
    }

    public void ClearParameters()
    {
        _currentParameters.Clear();
    }

    private SqlCommand CreateStoredProcedureCommand(string storeName)
    {
        var cmd = new SqlCommand(storeName, (SqlConnection)_connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        if (_transaction != null)
        {
            cmd.Transaction = (SqlTransaction)_transaction;
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
                using var reader = await ((SqlCommand)_command).ExecuteReaderAsync(token);
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

    // Bản "fast" của ExecStoreToListObjectAsync — đọc thẳng SqlDataReader bằng CompiledReaderMapper,
    // KHÔNG dựng DataTable trung gian. SQL Server không có refcursor, đơn giản nhất trong 3 provider.
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
                using var reader = await ((SqlCommand)_command).ExecuteReaderAsync(token);
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
            int result = await ((SqlCommand)_command).ExecuteNonQueryAsync(token);
            ClearParameters();
            return result;
        }
    }

    public async Task<string> ExecuteNonQueryAsStringAsync(string storeName, CancellationToken cancellationToken = default)
    {
        using var timeoutCts = CancellationTokenTimeoutHelper.CreateLinkedTimeoutSource(cancellationToken);
        var token = timeoutCts.Token;

        await EnsureOpenConnectionAsync(token);

        try
        {
            using (_command = CreateStoredProcedureCommand(storeName))
            {
                object result = await ((SqlCommand)_command).ExecuteScalarAsync(token);
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
