using System.Data;
using ApiCore8.Infrastructure.Database;
using Xunit;

namespace ApiCore8.UnitTests.Database;

public class DataRowMapperTests
{
    private class TestUser
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private static DataTable BuildTable()
    {
        // Cột đặt tên snake_case (is_active, createdat viết liền cũng test luôn 1 kiểu khác) —
        // khớp đúng thực tế cột SQL trả về, không phải test riêng cho tiện.
        var table = new DataTable();
        table.Columns.Add("id", typeof(Guid));
        table.Columns.Add("username", typeof(string));
        table.Columns.Add("is_active", typeof(bool));
        table.Columns.Add("createdat", typeof(DateTime));
        return table;
    }

    [Fact]
    public void GetItem_MapsMatchingColumnsCaseInsensitively()
    {
        var table = BuildTable();
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var row = table.NewRow();
        row["id"] = id;
        row["username"] = "hieu";
        row["is_active"] = true;
        row["createdat"] = now;
        table.Rows.Add(row);

        var result = DataRowMapper.GetItem<TestUser>(table.Rows[0]);

        Assert.Equal(id, result.Id);
        Assert.Equal("hieu", result.Username);
        Assert.True(result.IsActive);
        Assert.Equal(now, result.CreatedAt);
    }

    private class UsersLike
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    [Fact]
    public void GetItem_SnakeCaseColumns_MapToPascalCaseProperties()
    {
        // Bug thật đã gặp: cột SQL "created_at"/"updated_at"/"full_name"/"password_hash"/"is_active"
        // (snake_case, có dấu "_") không khớp property C# "CreatedAt"/"UpdatedAt"/... (PascalCase,
        // không "_") nếu chỉ so sánh lowercase thường — kết quả im lặng bỏ qua, property giữ giá trị
        // default (DateTime -> "0001-01-01T00:00:00", string -> ""), không throw, rất khó phát hiện.
        var table = new DataTable();
        table.Columns.Add("id", typeof(Guid));
        table.Columns.Add("username", typeof(string));
        table.Columns.Add("password_hash", typeof(string));
        table.Columns.Add("full_name", typeof(string));
        table.Columns.Add("email", typeof(string));
        table.Columns.Add("is_active", typeof(bool));
        table.Columns.Add("created_at", typeof(DateTime));
        table.Columns.Add("updated_at", typeof(DateTime));

        var id = Guid.NewGuid();
        var createdAt = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2026, 1, 2, 11, 0, 0, DateTimeKind.Utc);
        var row = table.NewRow();
        row["id"] = id;
        row["username"] = "hieu";
        row["password_hash"] = "hash";
        row["full_name"] = "Nguyen Trung Hieu";
        row["email"] = "hieu@example.com";
        row["is_active"] = true;
        row["created_at"] = createdAt;
        row["updated_at"] = updatedAt;
        table.Rows.Add(row);

        var result = DataRowMapper.GetItem<UsersLike>(table.Rows[0]);

        Assert.Equal(id, result.Id);
        Assert.Equal("hash", result.PasswordHash);
        Assert.Equal("Nguyen Trung Hieu", result.FullName);
        Assert.True(result.IsActive);
        Assert.Equal(createdAt, result.CreatedAt);
        Assert.Equal(updatedAt, result.UpdatedAt);
        Assert.NotEqual(default, result.CreatedAt); // không còn "0001-01-01" mặc định
        Assert.NotEqual(default, result.UpdatedAt);
    }

    [Theory]
    [InlineData(DateTimeKind.Unspecified)] // SqlClient/Oracle mặc định trả về kiểu này dù dữ liệu là UTC
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Utc)]         // Npgsql đã tự trả đúng sẵn
    public void GetItem_DateTimeProperty_AlwaysForcedToUtcKind(DateTimeKind driverReturnedKind)
    {
        // Bug thật: SqlClient/Oracle trả DateTime với Kind=Unspecified dù giá trị luôn là UTC (do
        // toàn hệ thống chỉ insert UtcNow/SYSUTCDATETIME/SYSTIMESTAMP) -> nếu không ép lại Kind=Utc,
        // System.Text.Json serialize thiếu hậu tố "Z", client (JS) hiểu nhầm là giờ local, convert sai.
        var table = new DataTable();
        table.Columns.Add("id", typeof(Guid));
        table.Columns.Add("createdat", typeof(DateTime));
        var rawValue = DateTime.SpecifyKind(new DateTime(2026, 8, 29, 9, 22, 40), driverReturnedKind);
        var row = table.NewRow();
        row["id"] = Guid.NewGuid();
        row["createdat"] = rawValue;
        table.Rows.Add(row);

        var result = DataRowMapper.GetItem<TestUser>(table.Rows[0]);

        Assert.Equal(DateTimeKind.Utc, result.CreatedAt.Kind);
        Assert.Equal(rawValue.Ticks, result.CreatedAt.Ticks); // chỉ đổi Kind, không đổi giá trị giờ
    }

    [Fact]
    public void GetItem_GuidAsString_ParsesCorrectly()
    {
        // Oracle không có kiểu UUID native, lưu id dạng VARCHAR2 (chuỗi hex 32 ký tự, không dấu gạch)
        // -> phải Guid.Parse được, không cast thẳng như Postgres/SQL Server (đã trả sẵn System.Guid).
        var table = BuildTable();
        var id = Guid.NewGuid();
        var row = table.NewRow();
        row["id"] = id.ToString("N"); // dạng Oracle trả về: 32 hex, không gạch ngang
        row["username"] = "hieu";
        row["is_active"] = true;
        row["createdat"] = DateTime.UtcNow;
        table.Rows.Add(row);

        var result = DataRowMapper.GetItem<TestUser>(table.Rows[0]);

        Assert.Equal(id, result.Id);
    }

    [Fact]
    public void GetItem_DbNullColumn_LeavesPropertyDefault()
    {
        var table = BuildTable();
        var row = table.NewRow();
        row["id"] = Guid.NewGuid();
        row["username"] = DBNull.Value;
        row["is_active"] = false;
        row["createdat"] = DateTime.UtcNow;
        table.Rows.Add(row);

        var result = DataRowMapper.GetItem<TestUser>(table.Rows[0]);

        Assert.Equal(string.Empty, result.Username); // default, không bị gán DBNull
    }

    [Fact]
    public void ConvertDataTableToList_MapsEveryRow()
    {
        var table = BuildTable();
        for (int i = 0; i < 3; i++)
        {
            var row = table.NewRow();
            row["id"] = Guid.NewGuid();
            row["username"] = $"user{i}";
            row["is_active"] = i % 2 == 0;
            row["createdat"] = DateTime.UtcNow;
            table.Rows.Add(row);
        }

        var result = DataRowMapper.ConvertDataTableToList<TestUser>(table);

        Assert.Equal(3, result.Count);
        Assert.Equal("user0", result[0].Username);
        Assert.Equal("user1", result[1].Username);
        Assert.Equal("user2", result[2].Username);
    }

    [Fact]
    public void ConvertDataTableToList_EmptyTable_ReturnsEmptyList()
    {
        var table = BuildTable();

        var result = DataRowMapper.ConvertDataTableToList<TestUser>(table);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetItem_ExtraColumnWithNoMatchingProperty_IsIgnored()
    {
        var table = BuildTable();
        table.Columns.Add("some_column_no_property_matches", typeof(string));
        var row = table.NewRow();
        row["id"] = Guid.NewGuid();
        row["username"] = "hieu";
        row["is_active"] = true;
        row["createdat"] = DateTime.UtcNow;
        row["some_column_no_property_matches"] = "bất kỳ giá trị gì";
        table.Rows.Add(row);

        // Không throw dù có cột dư không khớp property nào của TestUser.
        var result = DataRowMapper.GetItem<TestUser>(table.Rows[0]);

        Assert.Equal("hieu", result.Username);
    }

    private class TestUserWithNullable
    {
        public Guid Id { get; set; }
        public int? LoginCount { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    [Fact]
    public void GetItem_NullableProperty_WithValue_MapsCorrectly()
    {
        var table = new DataTable();
        table.Columns.Add("id", typeof(Guid));
        table.Columns.Add("logincount", typeof(int));
        table.Columns.Add("lastloginat", typeof(DateTime));
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var row = table.NewRow();
        row["id"] = id;
        row["logincount"] = 5;
        row["lastloginat"] = now;
        table.Rows.Add(row);

        var result = DataRowMapper.GetItem<TestUserWithNullable>(table.Rows[0]);

        Assert.Equal(5, result.LoginCount);
        Assert.Equal(now, result.LastLoginAt);
    }

    [Fact]
    public void GetItem_UnparsableGuidString_ThrowsWithColumnNameContext()
    {
        // Cột "id" typed string (giống Oracle trả về VARCHAR2 thô) để DataTable không tự coerce
        // trước khi tới DataRowMapper — verify chính Guid.Parse trong DataRowMapper báo lỗi rõ ràng.
        var table = new DataTable();
        table.Columns.Add("id", typeof(string));
        table.Columns.Add("username", typeof(string));
        var row = table.NewRow();
        row["id"] = "khong-phai-guid-hop-le";
        row["username"] = "hieu";
        table.Rows.Add(row);

        var ex = Assert.Throws<InvalidOperationException>(() => DataRowMapper.GetItem<TestUser>(table.Rows[0]));
        Assert.Contains("id", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private class TestUserWithIntProperty
    {
        public Guid Id { get; set; }
        public int Age { get; set; }
    }

    [Fact]
    public void GetItem_UnconvertibleValue_ThrowsWithColumnNameContext()
    {
        var table = new DataTable();
        table.Columns.Add("id", typeof(Guid));
        table.Columns.Add("age", typeof(string));
        var row = table.NewRow();
        row["id"] = Guid.NewGuid();
        row["age"] = "không phải số"; // không Convert.ChangeType được sang int
        table.Rows.Add(row);

        var ex = Assert.Throws<InvalidOperationException>(() => DataRowMapper.GetItem<TestUserWithIntProperty>(table.Rows[0]));
        Assert.Contains("age", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetItem_NullableProperty_DbNull_LeavesNull()
    {
        var table = new DataTable();
        table.Columns.Add("id", typeof(Guid));
        table.Columns.Add("logincount", typeof(int));
        table.Columns.Add("lastloginat", typeof(DateTime));
        var row = table.NewRow();
        row["id"] = Guid.NewGuid();
        row["logincount"] = DBNull.Value;
        row["lastloginat"] = DBNull.Value;
        table.Rows.Add(row);

        var result = DataRowMapper.GetItem<TestUserWithNullable>(table.Rows[0]);

        Assert.Null(result.LoginCount);
        Assert.Null(result.LastLoginAt);
    }
}
