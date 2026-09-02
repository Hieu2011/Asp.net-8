using System.Data;
using ApiCore8.Infrastructure.Database;
using Xunit;

namespace ApiCore8.UnitTests.Database;

public class CompiledReaderMapperTests
{
    private class TestUser
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // Dùng DataTable.CreateDataReader() để dựng 1 IDataReader thật (không phải fake tay) — verify
    // đúng hành vi thật của IDataRecord.GetGuid/GetString/GetBoolean/GetDateTime/IsDBNull.
    private static IDataReader CreateReader(DataTable table) => table.CreateDataReader();

    [Fact]
    public void Build_SnakeCaseColumns_MapToPascalCaseProperties()
    {
        var table = new DataTable();
        table.Columns.Add("id", typeof(Guid));
        table.Columns.Add("username", typeof(string));
        table.Columns.Add("is_active", typeof(bool));
        table.Columns.Add("created_at", typeof(DateTime));

        var id = Guid.NewGuid();
        var createdAt = new DateTime(2026, 8, 29, 10, 0, 0, DateTimeKind.Utc);
        var row = table.NewRow();
        row["id"] = id;
        row["username"] = "hieu";
        row["is_active"] = true;
        row["created_at"] = createdAt;
        table.Rows.Add(row);

        using var reader = CreateReader(table);
        var mapper = CompiledReaderMapper.Build<TestUser>(reader);
        reader.Read();
        var result = mapper(reader);

        Assert.Equal(id, result.Id);
        Assert.Equal("hieu", result.Username);
        Assert.True(result.IsActive);
        Assert.Equal(createdAt, result.CreatedAt);
    }

    [Fact]
    public void Build_GuidAsString_ParsesCorrectly()
    {
        // Oracle không có UUID native, trả VARCHAR2 (string) — GuidHelper.ToGuid phải tự Guid.Parse.
        var table = new DataTable();
        table.Columns.Add("id", typeof(string));
        table.Columns.Add("username", typeof(string));
        table.Columns.Add("is_active", typeof(bool));
        table.Columns.Add("created_at", typeof(DateTime));

        var id = Guid.NewGuid();
        var row = table.NewRow();
        row["id"] = id.ToString("N"); // dạng Oracle trả về: 32 hex, không gạch ngang
        row["username"] = "hieu";
        row["is_active"] = true;
        row["created_at"] = DateTime.UtcNow;
        table.Rows.Add(row);

        using var reader = CreateReader(table);
        var mapper = CompiledReaderMapper.Build<TestUser>(reader);
        reader.Read();
        var result = mapper(reader);

        Assert.Equal(id, result.Id);
    }

    [Theory]
    [InlineData(DateTimeKind.Unspecified)] // SqlClient/Oracle trả về kiểu này dù dữ liệu là UTC
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Utc)]
    public void Build_DateTimeProperty_AlwaysForcedToUtcKind(DateTimeKind driverReturnedKind)
    {
        var table = new DataTable();
        table.Columns.Add("id", typeof(Guid));
        table.Columns.Add("username", typeof(string));
        table.Columns.Add("is_active", typeof(bool));
        table.Columns.Add("created_at", typeof(DateTime));

        var rawValue = DateTime.SpecifyKind(new DateTime(2026, 8, 29, 9, 22, 40), driverReturnedKind);
        var row = table.NewRow();
        row["id"] = Guid.NewGuid();
        row["username"] = "hieu";
        row["is_active"] = true;
        row["created_at"] = rawValue;
        table.Rows.Add(row);

        using var reader = CreateReader(table);
        var mapper = CompiledReaderMapper.Build<TestUser>(reader);
        reader.Read();
        var result = mapper(reader);

        Assert.Equal(DateTimeKind.Utc, result.CreatedAt.Kind);
        Assert.Equal(rawValue.Ticks, result.CreatedAt.Ticks);
    }

    [Fact]
    public void Build_DbNullColumn_LeavesPropertyDefault()
    {
        var table = new DataTable();
        table.Columns.Add("id", typeof(Guid));
        table.Columns.Add("username", typeof(string));
        table.Columns.Add("is_active", typeof(bool));
        table.Columns.Add("created_at", typeof(DateTime));

        var row = table.NewRow();
        row["id"] = Guid.NewGuid();
        row["username"] = DBNull.Value;
        row["is_active"] = false;
        row["created_at"] = DateTime.UtcNow;
        table.Rows.Add(row);

        using var reader = CreateReader(table);
        var mapper = CompiledReaderMapper.Build<TestUser>(reader);
        reader.Read();
        var result = mapper(reader);

        Assert.Equal(string.Empty, result.Username); // default(string), không throw
    }

    [Fact]
    public void Build_ExtraColumnWithNoMatchingProperty_IsIgnored()
    {
        var table = new DataTable();
        table.Columns.Add("id", typeof(Guid));
        table.Columns.Add("username", typeof(string));
        table.Columns.Add("is_active", typeof(bool));
        table.Columns.Add("created_at", typeof(DateTime));
        table.Columns.Add("some_column_no_property_matches", typeof(string));

        var row = table.NewRow();
        row["id"] = Guid.NewGuid();
        row["username"] = "hieu";
        row["is_active"] = true;
        row["created_at"] = DateTime.UtcNow;
        row["some_column_no_property_matches"] = "bất kỳ giá trị gì";
        table.Rows.Add(row);

        using var reader = CreateReader(table);
        var mapper = CompiledReaderMapper.Build<TestUser>(reader);
        reader.Read();
        var result = mapper(reader);

        Assert.Equal("hieu", result.Username);
    }

    [Fact]
    public void Build_MultipleRows_MapsEachRowIndependently()
    {
        var table = new DataTable();
        table.Columns.Add("id", typeof(Guid));
        table.Columns.Add("username", typeof(string));
        table.Columns.Add("is_active", typeof(bool));
        table.Columns.Add("created_at", typeof(DateTime));

        for (int i = 0; i < 3; i++)
        {
            var row = table.NewRow();
            row["id"] = Guid.NewGuid();
            row["username"] = $"user{i}";
            row["is_active"] = i % 2 == 0;
            row["created_at"] = DateTime.UtcNow;
            table.Rows.Add(row);
        }

        using var reader = CreateReader(table);
        var mapper = CompiledReaderMapper.Build<TestUser>(reader);
        var results = new List<TestUser>();
        while (reader.Read())
        {
            results.Add(mapper(reader));
        }

        Assert.Equal(3, results.Count);
        Assert.Equal("user0", results[0].Username);
        Assert.Equal("user1", results[1].Username);
        Assert.Equal("user2", results[2].Username);
    }
}
