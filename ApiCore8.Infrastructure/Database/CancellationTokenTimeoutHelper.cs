namespace ApiCore8.Infrastructure.Database
{
    /// <summary>
    /// Đảm bảo MỌI thao tác DB luôn có 1 giới hạn thời gian tối thiểu — kể cả khi caller không
    /// truyền CancellationToken nào (CancellationToken.None mặc định KHÔNG BAO GIỜ tự hủy).
    /// Link token caller truyền vào (nếu có) với 1 deadline nội bộ — cái nào hủy trước thì thắng:
    /// caller hủy sớm hơn deadline -> theo caller; caller không truyền/không hủy -> tự cắt sau
    /// DefaultTimeout. Đây là lớp bảo vệ Ở TẦNG IDataCore, độc lập với timeout khai báo trong
    /// connection string (Command Timeout của Npgsql/SqlClient/Oracle) — 2 cơ chế khác nhau,
    /// cái nào tới hạn trước thì cắt trước.
    /// </summary>
    public static class CancellationTokenTimeoutHelper
    {
        // Đồng bộ với "Command Timeout=10" trong connection string Postgres — tránh 2 lớp timeout
        // giẫm chân nhau (lớp nào ngắn hơn sẽ luôn cắt trước, khiến lớp còn lại vô nghĩa).
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

        public static CancellationTokenSource CreateLinkedTimeoutSource(CancellationToken cancellationToken, TimeSpan? timeout = null)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout ?? DefaultTimeout);
            return cts;
        }
    }
}
