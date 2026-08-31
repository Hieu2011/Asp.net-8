using ApiCore8.Infrastructure.Database;
using ApiCore8.Infrastructure.Oracle;
using ApiCore8.Infrastructure.Postgres;
using ApiCore8.Infrastructure.SqlServer;
using Xunit;

namespace ApiCore8.UnitTests.Database;

/// <summary>
/// Verify đúng dây nối quan trọng nhất: connection string -> ConnectionStringDetector -> đúng
/// class IDataCore. Đây chính là factory dùng thật trong DependencyInjection.AddInfrastructureServices,
/// tách riêng ra để test không cần dựng cả DI container.
/// </summary>
public class DataCoreFactoryTests
{
    [Fact]
    public void Create_PostgresConnectionString_ReturnsPostgresDbHelper()
    {
        var result = DataCoreFactory.Create("Host=localhost;Port=5432;Username=x;Password=y;Database=z");

        Assert.IsType<PostgresDbHelper>(result);
    }

    [Fact]
    public void Create_OracleConnectionString_ReturnsOracleDbHelper()
    {
        var result = DataCoreFactory.Create("Data Source=localhost:1521/orcl;User Id=system;Password=y");

        Assert.IsType<OracleDbHelper>(result);
    }

    [Fact]
    public void Create_SqlServerConnectionString_ReturnsSqlServerDbHelper()
    {
        var result = DataCoreFactory.Create("Data Source=myServer;Initial Catalog=mydb;User ID=sa;Password=y");

        Assert.IsType<SqlServerDbHelper>(result);
    }

    [Fact]
    public void Create_RealProjectPostgresConnectionString_ReturnsPostgresDbHelper()
    {
        // Đúng connection string thật đang dùng trong User Secrets (alias cũ Server/User ID/Database)
        // -> chốt lại luôn, không chỉ test ở tầng Detector mà test cả tới tận factory thật.
        var result = DataCoreFactory.Create(
            "Server=192.168.48.162;Port=5432;Database=HPM;User ID=postgres;Password=xxx;Timeout=5;Command Timeout=10");

        Assert.IsType<PostgresDbHelper>(result);
    }

    [Fact]
    public void Create_UnrecognizableConnectionString_Throws()
    {
        Assert.Throws<NotSupportedException>(() => DataCoreFactory.Create("foo=bar"));
    }
}
