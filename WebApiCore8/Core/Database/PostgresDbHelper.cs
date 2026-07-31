using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Npgsql;
using NpgsqlTypes;
using Core.Database;
using Core;

public class PostgresDbHelper : IDisposable, IDataCore
{
    private readonly string _connectionString;
    private IDbConnection _connection;
    private IDbTransaction _transaction;
    private IDbCommand _command;
    private List<NpgsqlParameter> _currentParameters = new();
    private static readonly ConcurrentDictionary<string, PropertyInfo[]> _propertyCache = new ConcurrentDictionary<string, PropertyInfo[]>();

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

    public PostgresDbHelper(string connectionString = "")
    {
        _connectionString = string.IsNullOrEmpty(connectionString)
            ? ConfigHelper.GetConnectionString()
            : connectionString;
        _connection = new NpgsqlConnection(_connectionString);
    }

    // Mở kết nối nếu chưa mở
    private async Task EnsureOpenConnectionAsync()
    {
        if (_connection == null)
        {
            _connection = new NpgsqlConnection(_connectionString);
        }

        if (_connection.State == ConnectionState.Closed || _connection.State == ConnectionState.Broken)
        {
            try
            {
                await ((NpgsqlConnection)_connection).OpenAsync();
            }
            catch (NpgsqlException)
            {
                // Thử tạo kết nối mới nếu không mở được kết nối cũ
                _connection.Dispose();
                _connection = new NpgsqlConnection(_connectionString);
                await ((NpgsqlConnection)_connection).OpenAsync();
            }
        }
    }

    // Bắt đầu transaction
    public async Task StartTransactionScopeAsync()
    {
        await EnsureOpenConnectionAsync();
        _transaction = await ((NpgsqlConnection)_connection).BeginTransactionAsync();
    }

    // Commit transaction
    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await ((NpgsqlTransaction)_transaction).CommitAsync();
            _transaction.Dispose();
            _transaction = null;
        }
    }

    // Rollback transaction
    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await ((NpgsqlTransaction)_transaction).RollbackAsync();
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
            param.NpgsqlDbType = NpgsqlDbType.Timestamp;
        }
        else if (value is bool)
        {
            param.NpgsqlDbType = NpgsqlDbType.Boolean;
        }
        else if (value is int[])
        {
            param.NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Integer;
        }

        // Xác định hướng tham số
        if (paramName.EndsWith("_out") || paramName == "ref_cursor")
        {
            param.Direction = ParameterDirection.InputOutput;
        }

        _currentParameters.Add(param);
    }

    // Xóa tham số
    public void ClearParameters()
    {
        _currentParameters.Clear();
    }

    // Lấy thuộc tính của kiểu dữ liệu và cache lại
    private static PropertyInfo[] GetTypeProperties<T>()
    {
        string typeName = typeof(T).FullName;
        return _propertyCache.GetOrAdd(typeName, _ => typeof(T).GetProperties());
    }

    // Ánh xạ từ DataRow sang object
    private static T GetItem<T>(DataRow dr)
    {
        T obj = Activator.CreateInstance<T>();
        PropertyInfo[] properties = GetTypeProperties<T>();

        // Tạo dictionary để map tên cột và property một lần
        var propertyMap = properties.ToDictionary(
            p => p.Name.ToLower(),
            p => p
        );

        foreach (DataColumn column in dr.Table.Columns)
        {
            string columnNameLower = column.ColumnName.ToLower();
            if (propertyMap.TryGetValue(columnNameLower, out PropertyInfo property))
            {
                try
                {
                    var data = dr[column.ColumnName];
                    if (data == DBNull.Value || data?.ToString() == "")
                    {
                        continue; // Bỏ qua giá trị null
                    }

                    // Xử lý kiểu nullable
                    Type propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                    // Convert dữ liệu và gán vào property
                    object convertedValue = Convert.ChangeType(data, propertyType);
                    property.SetValue(obj, convertedValue, null);
                }
                catch (Exception exception)
                {
                    throw new Exception($"Error Column: {column.ColumnName} || " + exception.Message);
                }
            }
        }
        return obj;
    }

    // Thực thi stored procedure trả về DataTable (refcursor)
    public async Task<DataTable> ExecuteStoreDataTableAsync(string storeName)
    {
        await EnsureOpenConnectionAsync();
        DataTable dt = new DataTable();
        string cursorName = storeName;
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

            var sqlBuilder = new StringBuilder();
            sqlBuilder.Append("SELECT ");
            sqlBuilder.Append(storeName);
            sqlBuilder.Append("(");
            sqlBuilder.Append(string.Join(", ", _currentParameters.Select(p => "@" + p.ParameterName)));
            sqlBuilder.Append(");");
            string sql = sqlBuilder.ToString();

            using (_command = new NpgsqlCommand(sql, (NpgsqlConnection)_connection, (NpgsqlTransaction)_transaction))
            {
                ((NpgsqlCommand)_command).Parameters.AddRange(_currentParameters.ToArray());

                using (var reader = await ((NpgsqlCommand)_command).ExecuteReaderAsync())
                {
                    if (!await reader.ReadAsync() || reader.IsDBNull(0))
                        return dt;
                    cursorName = reader.GetString(0);
                }
            }

            using (var fetchCmd = new NpgsqlCommand($"FETCH ALL IN \"{cursorName}\";", (NpgsqlConnection)_connection, (NpgsqlTransaction)_transaction))
            using (var fetchReader = await fetchCmd.ExecuteReaderAsync())
            {
                dt.Load(fetchReader);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing store {storeName}: {ex.Message}");
            throw;
        }
        finally
        {
            if (!string.IsNullOrEmpty(cursorName))
            {
                try
                {
                    using var closeCmd = new NpgsqlCommand($"CLOSE \"{cursorName}\";", (NpgsqlConnection)_connection, (NpgsqlTransaction)_transaction);
                    await closeCmd.ExecuteNonQueryAsync();
                }
                catch { }
            }
            ClearParameters();
        }
        return dt;
    }

    // Thực thi stored procedure trả về object
    public async Task<T> ExecStoreToObjectAsync<T>(string storeName)
    {
        var dataTable = await ExecuteStoreDataTableAsync(storeName);
        return dataTable?.Rows.Count > 0
            ? GetItem<T>(dataTable.Rows[0])
            : Activator.CreateInstance<T>();
    }

    // Thực thi stored procedure trả về list object
    public async Task<List<T>> ExecStoreToListObjectAsync<T>(string storeName)
    {
        var dataTable = await ExecuteStoreDataTableAsync(storeName);
        return dataTable?.Rows.Count > 0
            ? ConvertDataTableToList<T>(dataTable)
            : new List<T>();
    }

    // Chuyển đổi DataTable thành List<T>
    public static List<T> ConvertDataTableToList<T>(DataTable dt)
    {
        List<T> data = new List<T>();
        foreach (DataRow row in dt.Rows)
        {
            var result = GetItem<T>(row);
            data.Add(result);
        }
        return data;
    }

    // Thực thi stored procedure không trả về dữ liệu
    public async Task<int> ExecuteNonQueryAsync(string storeName)
    {
        await EnsureOpenConnectionAsync();

        // Sử dụng StringBuilder thay vì nối chuỗi
        var sqlBuilder = new StringBuilder();
        sqlBuilder.Append("SELECT ");
        sqlBuilder.Append(storeName);
        sqlBuilder.Append("(");

        if (_currentParameters.Any())
        {
            sqlBuilder.Append(string.Join(", ", _currentParameters.Select(p => p.ParameterName)));
        }

        sqlBuilder.Append(");");
        string sql = sqlBuilder.ToString();

        using (_command = new NpgsqlCommand(sql, (NpgsqlConnection)_connection, (NpgsqlTransaction)_transaction))
        {
            if (_currentParameters.Any())
            {
                ((NpgsqlCommand)_command).Parameters.AddRange(_currentParameters.ToArray());
            }

            int result = await ((NpgsqlCommand)_command).ExecuteNonQueryAsync();
            ClearParameters();
            return result;
        }
    }
    // Thực thi stored procedure trả về chuỗi kết quả
    public async Task<string> ExecuteNonQueryAsStringAsync(string storeName)
    {
        await EnsureOpenConnectionAsync();

        // Tạo câu truy vấn
        var sqlBuilder = new StringBuilder();
        sqlBuilder.Append("SELECT ");
        sqlBuilder.Append(storeName);
        sqlBuilder.Append("(");

        if (_currentParameters.Any())
        {
            sqlBuilder.Append(string.Join(", ", _currentParameters.Select(p => p.ParameterName)));
        }

        sqlBuilder.Append(");");
        string sql = sqlBuilder.ToString();

        try
        {
            using (_command = new NpgsqlCommand(sql, (NpgsqlConnection)_connection, (NpgsqlTransaction)_transaction))
            {
                if (_currentParameters.Any())
                {
                    ((NpgsqlCommand)_command).Parameters.AddRange(_currentParameters.ToArray());
                }

                // Thực thi và đọc kết quả
                object result = await ((NpgsqlCommand)_command).ExecuteScalarAsync();
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