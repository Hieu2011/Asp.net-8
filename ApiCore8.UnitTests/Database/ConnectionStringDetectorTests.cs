using ApiCore8.Infrastructure.Database;
using Xunit;

namespace ApiCore8.UnitTests.Database;

public class ConnectionStringDetectorTests
{
    [Theory]
    [InlineData("Host=localhost;Port=5432;Username=postgres;Password=x;Database=mydb", DbProvider.Postgres)]
    [InlineData(@"Server=myServer\SQLEXPRESS;Database=mydb;User Id=sa;Password=x;TrustServerCertificate=True", DbProvider.SqlServer)]
    [InlineData("Data Source=myServer;Initial Catalog=mydb;User ID=sa;Password=x", DbProvider.SqlServer)]
    [InlineData("Data Source=localhost:1521/orcl;User Id=system;Password=x", DbProvider.Oracle)]
    [InlineData("Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=orcl)));User Id=system;Password=x", DbProvider.Oracle)]
    // Connection string thật đang dùng trong ApiCore8.Api User Secrets — Npgsql alias cũ
    // (Server/User ID/Database trùng tên key với SqlClient) — bug thật đã bắt được và fix.
    [InlineData("Server=192.168.48.162;Port=5432;Database=HPM;User ID=postgres;Password=xxx;Timeout=5;Command Timeout=10", DbProvider.Postgres)]
    public void Detect_ReturnsExpectedProvider(string connectionString, DbProvider expected)
    {
        var actual = ConnectionStringDetector.Detect(connectionString);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Detect_EmptyOrWhitespace_Throws(string? connectionString)
    {
        Assert.Throws<ArgumentException>(() => ConnectionStringDetector.Detect(connectionString!));
    }

    [Fact]
    public void Detect_UnrecognizableString_ThrowsNotSupported()
    {
        Assert.Throws<NotSupportedException>(() => ConnectionStringDetector.Detect("foo=bar;baz=qux"));
    }

    [Theory]
    [InlineData("host=localhost;username=x;password=y")] // lowercase toàn bộ
    [InlineData("HOST=localhost;USERNAME=x;PASSWORD=y")] // uppercase toàn bộ
    [InlineData("Host = localhost ; Username = x")]       // khoảng trắng quanh dấu "="
    public void Detect_Postgres_IsCaseAndWhitespaceInsensitive(string connectionString)
    {
        Assert.Equal(DbProvider.Postgres, ConnectionStringDetector.Detect(connectionString));
    }

    [Fact]
    public void Detect_Postgres_OnlyHostSignal_NoUsernameNoPort()
    {
        // Chỉ 1 tín hiệu Postgres duy nhất (Host=) vẫn phải nhận diện đúng, không cần cả 3 tín hiệu.
        Assert.Equal(DbProvider.Postgres, ConnectionStringDetector.Detect("Host=localhost;Database=x"));
    }

    [Fact]
    public void Detect_Postgres_OnlyPortSignal_LegacyServerAlias()
    {
        // Chính case bug thật đã bắt: alias cũ Server/User ID/Database, chỉ "Port=" phân biệt được
        // với SqlServer (SqlClient không có keyword Port riêng).
        Assert.Equal(DbProvider.Postgres,
            ConnectionStringDetector.Detect("Server=localhost;Port=5432;Database=x;User ID=y;Password=z"));
    }

    [Fact]
    public void Detect_Oracle_EzConnect_WithoutExplicitPort()
    {
        // EZ Connect không nhất thiết phải có port (host/service_name) — vẫn phải nhận ra nhờ dấu "/".
        Assert.Equal(DbProvider.Oracle, ConnectionStringDetector.Detect("Data Source=myhost/orcl;User Id=system;Password=x"));
    }

    [Fact]
    public void Detect_SqlServer_DataSourceWithoutInitialCatalog_FallsBackCorrectly()
    {
        // "Data Source=" không có dấu "/" (không phải Oracle) và không có Port (không phải Postgres)
        // -> phải rơi đúng vào nhánh SqlServer, dù không có "Initial Catalog=".
        Assert.Equal(DbProvider.SqlServer, ConnectionStringDetector.Detect("Data Source=myServer;User Id=sa;Password=x"));
    }
}
