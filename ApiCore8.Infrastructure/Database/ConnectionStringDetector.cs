using System.Text.RegularExpressions;

namespace ApiCore8.Infrastructure.Database
{
    public enum DbProvider
    {
        Postgres,
        Oracle,
        SqlServer
    }

    /// <summary>
    /// Nhận diện provider (Postgres/Oracle/SqlServer) từ nội dung connection string, dựa vào
    /// từ khóa đặc trưng riêng của từng driver — không có API chuẩn nào tự "đoán được hãng" 100%,
    /// vì cú pháp connection string do driver tự quy định, không có chuẩn chung.
    ///
    /// Độ tin cậy từng nhánh:
    /// - Postgres (Npgsql): "Host=" / "Username=" — 2 key Npgsql "hiện đại" dùng. NHƯNG Npgsql còn
    ///   hỗ trợ alias cũ "Server"/"User ID"/"Database" (trùng hệt tên key SqlClient!) — connection
    ///   string thật của project này đang dùng đúng dạng alias cũ này, nên chỉ check Host/Username
    ///   là KHÔNG ĐỦ (đã verify bằng test thật, xem lịch sử sửa). Dấu hiệu đáng tin cậy hơn: "Port="
    ///   là key riêng CHỈ Npgsql/MySQL dùng — SqlClient không có keyword "Port" (phải nhúng port vào
    ///   "Server=host,port"), Oracle cũng nhúng port vào Data Source (TNS/EZ Connect), không có
    ///   "Port=" riêng. → check thêm "Port=" để bắt được cả 2 dạng connection string Npgsql.
    /// - Oracle: "Data Source=" ở dạng TNS descriptor ("(DESCRIPTION=...)") hoặc EZ Connect
    ///   ("host:port/service_name" — có dấu "/" ngay trong giá trị Data Source). SQL Server không
    ///   bao giờ viết Data Source theo 2 dạng này. → Nhận diện khá chắc, nhưng phụ thuộc format
    ///   connection string Oracle thực tế đang dùng (nếu ai viết tay khác chuẩn có thể trượt).
    /// - SqlServer: "Server=" / "Initial Catalog=" / "Trusted_Connection=", hoặc "Data Source="
    ///   KHÔNG theo dạng Oracle ở trên, và KHÔNG có "Port=" (đã loại ở nhánh Postgres) → rơi vào
    ///   nhánh này. → Đây là nhánh "còn lại" nên kém chắc chắn nhất trong 3 nhánh.
    /// </summary>
    public static class ConnectionStringDetector
    {
        public static DbProvider Detect(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string rỗng — không thể nhận diện provider.", nameof(connectionString));
            }

            // Check Oracle TNS descriptor TRƯỚC Postgres: descriptor dạng "(DESCRIPTION=...(HOST=...)...)"
            // chứa sẵn "HOST=" bên trong (cú pháp TNS, không liên quan Npgsql) — nếu check Postgres
            // trước sẽ nhận nhầm "HOST=" đó thành Npgsql.
            bool hasDataSource = Regex.IsMatch(connectionString, @"(?i)\bData Source\s*=");
            bool looksLikeOracle =
                Regex.IsMatch(connectionString, @"(?i)\(DESCRIPTION\s*=") ||
                Regex.IsMatch(connectionString, @"(?i)Data Source\s*=\s*[^;]+/[^;]+");

            if (hasDataSource && looksLikeOracle)
            {
                return DbProvider.Oracle;
            }

            if (Regex.IsMatch(connectionString, @"(?i)\bHost\s*=") ||
                Regex.IsMatch(connectionString, @"(?i)\bUsername\s*=") ||
                Regex.IsMatch(connectionString, @"(?i)\bPort\s*="))
            {
                return DbProvider.Postgres;
            }

            if (Regex.IsMatch(connectionString, @"(?i)\bServer\s*=") ||
                Regex.IsMatch(connectionString, @"(?i)\bInitial Catalog\s*=") ||
                Regex.IsMatch(connectionString, @"(?i)\bTrusted_Connection\s*=") ||
                hasDataSource)
            {
                return DbProvider.SqlServer;
            }

            throw new NotSupportedException(
                "Không nhận diện được provider từ connection string. " +
                "Postgres cần có 'Host=' hoặc 'Username=' hoặc 'Port='. " +
                "Oracle cần 'Data Source=' dạng TNS ('(DESCRIPTION=...)') hoặc EZ Connect ('host:port/service_name'). " +
                "SQL Server cần 'Server=' hoặc 'Initial Catalog=' hoặc 'Trusted_Connection='.");
        }
    }
}
