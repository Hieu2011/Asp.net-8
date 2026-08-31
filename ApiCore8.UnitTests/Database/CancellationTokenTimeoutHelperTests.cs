using ApiCore8.Infrastructure.Database;
using Xunit;

namespace ApiCore8.UnitTests.Database;

public class CancellationTokenTimeoutHelperTests
{
    [Fact]
    public void CreateLinkedTimeoutSource_CallerNeverCancels_StillCancelsAfterTimeout()
    {
        // Đúng case anh hỏi: caller không truyền gì (CancellationToken.None, không bao giờ tự hủy)
        // -> vẫn phải tự cắt sau khoảng thời gian set (dùng 50ms thay vì DefaultTimeout 5s thật để
        // test chạy nhanh, logic hủy-theo-thời-gian giống hệt).
        using var cts = CancellationTokenTimeoutHelper.CreateLinkedTimeoutSource(CancellationToken.None, TimeSpan.FromMilliseconds(50));

        Assert.False(cts.Token.IsCancellationRequested); // chưa tới hạn

        Thread.Sleep(150);

        Assert.True(cts.Token.IsCancellationRequested); // đã tự hủy dù caller không truyền token nào
    }

    [Fact]
    public void CreateLinkedTimeoutSource_CallerCancelsFirst_HonorsCallerCancellation()
    {
        // Caller có truyền token thật và tự hủy SỚM HƠN deadline nội bộ -> phải theo caller ngay,
        // không đợi hết deadline nội bộ (5 giây thật/timeout dài trong test).
        using var callerCts = new CancellationTokenSource();
        using var linkedCts = CancellationTokenTimeoutHelper.CreateLinkedTimeoutSource(callerCts.Token, TimeSpan.FromSeconds(30));

        callerCts.Cancel();

        Assert.True(linkedCts.Token.IsCancellationRequested);
    }

    [Fact]
    public void CreateLinkedTimeoutSource_NoTimeoutSpecified_UsesDefaultTimeout()
    {
        // 10s — đồng bộ với "Command Timeout=10" trong connection string Postgres, tránh 2 lớp
        // timeout giẫm chân nhau.
        Assert.Equal(TimeSpan.FromSeconds(10), CancellationTokenTimeoutHelper.DefaultTimeout);
    }

    [Fact]
    public void CreateLinkedTimeoutSource_WithinDeadlineAndCallerNotCanceled_TokenStaysValid()
    {
        using var cts = CancellationTokenTimeoutHelper.CreateLinkedTimeoutSource(CancellationToken.None, TimeSpan.FromSeconds(10));

        Assert.False(cts.Token.IsCancellationRequested);
    }
}
