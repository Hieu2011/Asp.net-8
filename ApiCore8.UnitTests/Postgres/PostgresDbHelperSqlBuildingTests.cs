using ApiCore8.Infrastructure.Postgres;
using Npgsql;
using NpgsqlTypes;
using Xunit;

namespace ApiCore8.UnitTests.Postgres;

/// <summary>
/// Verify BuildCallSql dùng named notation ("tên => giá trị") — v_out (hay bất kỳ tham số nào)
/// nằm ở vị trí nào trong danh sách tham số cũng ra kết quả tương đương (chỉ khác thứ tự viết
/// trong câu SQL, không ảnh hưởng ý nghĩa vì PostgreSQL tự khớp theo tên, không theo vị trí).
/// </summary>
public class PostgresDbHelperSqlBuildingTests
{
    private static NpgsqlParameter P(string name) => new(name, DBNull.Value);

    [Fact]
    public void BuildCallSql_UsesNamedNotationForEveryParameter()
    {
        var parameters = new List<NpgsqlParameter>
        {
            new("v_out", DBNull.Value) { NpgsqlDbType = NpgsqlDbType.Refcursor },
            P("p_username"),
            P("p_password_hash"),
        };

        var sql = PostgresDbHelper.BuildCallSql("sp_user_create", parameters);

        Assert.Equal("SELECT sp_user_create(v_out => @v_out, p_username => @p_username, p_password_hash => @p_password_hash);", sql);
    }

    [Theory]
    [InlineData(0)] // v_out đầu
    [InlineData(1)] // v_out giữa
    [InlineData(2)] // v_out cuối
    public void BuildCallSql_VOutAtAnyPosition_StillProducesValidNamedCall(int vOutPosition)
    {
        var parameters = new List<NpgsqlParameter> { P("p_id"), P("p_full_name") };
        parameters.Insert(vOutPosition, new NpgsqlParameter("v_out", DBNull.Value) { NpgsqlDbType = NpgsqlDbType.Refcursor });

        var sql = PostgresDbHelper.BuildCallSql("sp_user_get_by_id", parameters);

        // Không quan tâm v_out nằm đâu trong chuỗi -> chỉ cần mỗi tham số đều ở dạng
        // "tên => @tên" (named notation), PostgreSQL tự khớp đúng theo tên bất kể thứ tự viết.
        Assert.Contains("v_out => @v_out", sql);
        Assert.Contains("p_id => @p_id", sql);
        Assert.Contains("p_full_name => @p_full_name", sql);
        Assert.StartsWith("SELECT sp_user_get_by_id(", sql);
        Assert.EndsWith(");", sql);
    }

    [Fact]
    public void BuildCallSql_NoParameters_ProducesEmptyArgumentList()
    {
        var sql = PostgresDbHelper.BuildCallSql("sp_user_get_all", new List<NpgsqlParameter>());

        Assert.Equal("SELECT sp_user_get_all();", sql);
    }
}
