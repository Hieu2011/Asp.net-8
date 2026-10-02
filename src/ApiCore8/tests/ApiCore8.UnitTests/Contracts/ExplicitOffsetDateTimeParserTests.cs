using ApiCore8.Application.Contracts;
using Xunit;

namespace ApiCore8.UnitTests.Contracts;

public class ExplicitOffsetDateTimeParserTests
{
    [Theory]
    [InlineData("2026-08-29T17:00:00+07:00")]
    [InlineData("2026-08-29T10:00:00Z")]
    [InlineData("2026-08-29T10:00:00+0000")] // offset không dấu ":"
    [InlineData("2026-08-29T03:00:00-05:00")]
    public void TryParse_ExplicitOffset_Succeeds(string input)
    {
        var success = ExplicitOffsetDateTimeParser.TryParse(input, out var result);

        Assert.True(success);
        Assert.NotEqual(default, result);
    }

    [Fact]
    public void TryParse_ExplicitOffsetPlus7_ConvertsToCorrectUtc()
    {
        ExplicitOffsetDateTimeParser.TryParse("2026-08-29T17:00:00+07:00", out var result);

        Assert.Equal(new DateTime(2026, 8, 29, 10, 0, 0, DateTimeKind.Utc), result.UtcDateTime);
    }

    [Theory]
    [InlineData("2026-08-29T17:00:00")]      // bug thật đã gặp: thiếu offset
    [InlineData("2026-08-29")]               // chỉ có ngày, không giờ lẫn offset
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("khong-phai-ngay-gio")]
    public void TryParse_MissingOrInvalidOffset_Fails(string? input)
    {
        var success = ExplicitOffsetDateTimeParser.TryParse(input, out var result);

        Assert.False(success);
        Assert.Equal(default, result);
    }
}
