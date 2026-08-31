using ApiCore8.Infrastructure.Oracle;
using ApiCore8.Infrastructure.Postgres;
using ApiCore8.Infrastructure.SqlServer;
using Microsoft.Data.SqlClient;
using NpgsqlTypes;
using Oracle.ManagedDataAccess.Client;
using Xunit;

namespace ApiCore8.UnitTests.Database;

/// <summary>
/// Verify AddParameter gán đúng kiểu DB-specific cho DateTime — bug thật đã bắt được: cả 3 driver
/// mặc định tự suy kiểu KHÔNG khớp cột DDL đang dùng (timestamptz/DATETIME2/TIMESTAMP) nếu không
/// ép rõ. Không cần DB thật — chỉ construct parameter, không mở connection.
/// </summary>
public class AddParameterTypeTests
{
    private const string DummyConnectionString = "Host=localhost;Port=5432;Username=x;Password=x;Database=x";
    private const string DummySqlConnectionString = "Server=localhost;Database=x;User Id=x;Password=x;TrustServerCertificate=True";
    private const string DummyOracleConnectionString = "Data Source=localhost:1521/xe;User Id=x;Password=x";

    [Fact]
    public void Postgres_AddParameter_DateTime_UsesTimestampTz()
    {
        using var helper = new PostgresDbHelper(DummyConnectionString);
        helper.AddParameter("p_created_at", DateTime.UtcNow);

        Assert.Equal(NpgsqlDbType.TimestampTz, helper._currentParameters[0].NpgsqlDbType);
    }

    [Fact]
    public void Postgres_AddParameter_Guid_UsesUuid()
    {
        using var helper = new PostgresDbHelper(DummyConnectionString);
        helper.AddParameter("p_id", Guid.NewGuid());

        Assert.Equal(NpgsqlDbType.Uuid, helper._currentParameters[0].NpgsqlDbType);
    }

    [Fact]
    public void SqlServer_AddParameter_DateTime_UsesDateTime2()
    {
        using var helper = new SqlServerDbHelper(DummySqlConnectionString);
        helper.AddParameter("p_created_at", DateTime.UtcNow);

        Assert.Equal(System.Data.SqlDbType.DateTime2, helper._currentParameters[0].SqlDbType);
    }

    [Fact]
    public void SqlServer_AddParameter_PrependsAtSign()
    {
        using var helper = new SqlServerDbHelper(DummySqlConnectionString);
        helper.AddParameter("p_id", Guid.NewGuid());

        Assert.Equal("@p_id", helper._currentParameters[0].ParameterName);
    }

    [Fact]
    public void Oracle_AddParameter_DateTime_UsesTimeStamp()
    {
        using var helper = new OracleDbHelper(DummyOracleConnectionString);
        helper.AddParameter("p_created_at", DateTime.UtcNow);

        Assert.Equal(OracleDbType.TimeStamp, helper._currentParameters[0].OracleDbType);
    }

    [Fact]
    public void Oracle_AddParameter_Bool_ConvertsToZeroOrOne()
    {
        using var helper = new OracleDbHelper(DummyOracleConnectionString);
        helper.AddParameter("p_is_active", true);
        helper.AddParameter("p_is_inactive", false);

        Assert.Equal(1, helper._currentParameters[0].Value);
        Assert.Equal(0, helper._currentParameters[1].Value);
    }

    [Fact]
    public void Postgres_AddParameter_Bool_UsesBoolean()
    {
        using var helper = new PostgresDbHelper(DummyConnectionString);
        helper.AddParameter("p_is_active", true);

        Assert.Equal(NpgsqlDbType.Boolean, helper._currentParameters[0].NpgsqlDbType);
        Assert.Equal(true, helper._currentParameters[0].Value); // không convert 1/0 như Oracle
    }

    [Fact]
    public void Postgres_AddParameter_IntArray_UsesArrayOfInteger()
    {
        using var helper = new PostgresDbHelper(DummyConnectionString);
        helper.AddParameter("p_ids", new[] { 1, 2, 3 });

        Assert.Equal(NpgsqlDbType.Array | NpgsqlDbType.Integer, helper._currentParameters[0].NpgsqlDbType);
    }

    [Fact]
    public void Postgres_AddParameter_NullValue_UsesDBNull()
    {
        using var helper = new PostgresDbHelper(DummyConnectionString);
        helper.AddParameter("p_optional", null!);

        Assert.Equal(DBNull.Value, helper._currentParameters[0].Value);
    }

    [Fact]
    public void Postgres_ClearParameters_RemovesAllAddedParameters()
    {
        using var helper = new PostgresDbHelper(DummyConnectionString);
        helper.AddParameter("p_a", 1);
        helper.AddParameter("p_b", 2);

        helper.ClearParameters();

        Assert.Empty(helper._currentParameters);
    }

    [Fact]
    public void SqlServer_AddParameter_AlreadyHasAtSign_DoesNotDoublePrefix()
    {
        using var helper = new SqlServerDbHelper(DummySqlConnectionString);
        helper.AddParameter("@p_id", Guid.NewGuid());

        Assert.Equal("@p_id", helper._currentParameters[0].ParameterName);
    }

    [Fact]
    public void SqlServer_ClearParameters_RemovesAllAddedParameters()
    {
        using var helper = new SqlServerDbHelper(DummySqlConnectionString);
        helper.AddParameter("p_a", 1);

        helper.ClearParameters();

        Assert.Empty(helper._currentParameters);
    }

    [Fact]
    public void Oracle_ClearParameters_RemovesAllAddedParameters()
    {
        using var helper = new OracleDbHelper(DummyOracleConnectionString);
        helper.AddParameter("p_a", 1);

        helper.ClearParameters();

        Assert.Empty(helper._currentParameters);
    }
}
