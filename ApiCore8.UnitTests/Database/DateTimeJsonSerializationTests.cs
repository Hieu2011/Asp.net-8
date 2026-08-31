using System.Data;
using System.Text.Json;
using ApiCore8.Infrastructure.Database;
using Xunit;

namespace ApiCore8.UnitTests.Database;

/// <summary>
/// Verify tận đầu ra JSON thật (System.Text.Json — mặc định của ASP.NET Core, project này KHÔNG
/// gọi AddNewtonsoftJson()) — không chỉ dừng ở việc DataRowMapper gán đúng Kind=Utc, mà phải thấy
/// tận mắt hậu tố "Z" xuất hiện trong chuỗi JSON, đúng thứ client (JS) cần để hiểu đúng là UTC.
/// </summary>
public class DateTimeJsonSerializationTests
{
    private class Entity
    {
        public DateTime CreatedAt { get; set; }
    }

    [Theory]
    [InlineData(DateTimeKind.Unspecified)] // SqlClient/Oracle trả về kiểu này trước khi DataRowMapper ép lại
    [InlineData(DateTimeKind.Local)]
    public void MappedDateTime_SerializesWithZSuffix_RegardlessOfDriverOriginalKind(DateTimeKind driverReturnedKind)
    {
        var table = new DataTable();
        table.Columns.Add("createdat", typeof(DateTime));
        var row = table.NewRow();
        row["createdat"] = DateTime.SpecifyKind(new DateTime(2026, 8, 29, 9, 22, 40, 139), driverReturnedKind);
        table.Rows.Add(row);

        var entity = DataRowMapper.GetItem<Entity>(table.Rows[0]);
        var json = JsonSerializer.Serialize(entity);

        Assert.Contains("Z\"", json); // "...139Z" — hậu tố Z ngay trước dấu " đóng chuỗi
        Assert.DoesNotContain("+07:00", json); // không bị lẫn offset local nào khác
    }

    [Fact]
    public void UnfixedUnspecifiedDateTime_WouldSerializeWithoutZSuffix_ProvingTheBugWasReal()
    {
        // Test này KHÔNG gọi qua DataRowMapper — dựng thẳng DateTime Kind=Unspecified để chứng minh
        // hành vi mặc định của System.Text.Json khi CHƯA có fix, làm rõ vì sao phải ép Kind=Utc.
        var raw = new { CreatedAt = DateTime.SpecifyKind(new DateTime(2026, 8, 29, 9, 22, 40), DateTimeKind.Unspecified) };

        var json = JsonSerializer.Serialize(raw);

        Assert.DoesNotContain("Z\"", json); // đúng như mô tả bug: thiếu hậu tố Z, mơ hồ với client
    }
}
