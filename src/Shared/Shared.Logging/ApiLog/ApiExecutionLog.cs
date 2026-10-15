using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Shared.Logging.ApiLog;

/// <summary>1 lần gọi API do [LogApi] ghi lại — lưu Mongo, thời gian UTC.</summary>
public class ApiExecutionLog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public string ApiName { get; set; } = string.Empty;
    public string ClientIP { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    /// <summary>Mã HTTP cuối cùng client nhận (sau exception filter/middleware), không phải lúc action kết thúc.</summary>
    public int StatusCode { get; set; }
    /// <summary>StatusCode ≥ 400 hoặc có exception — lọc nhanh các lần gọi lỗi.</summary>
    public bool IsError { get; set; }
    /// <summary>TraceId W3C (Activity) — trùng field TraceId của SystemLogs và "mã lỗi" trả cho client.</summary>
    public string TraceID { get; set; } = string.Empty;
    /// <summary>Tên đầy đủ của exception (VD AuthService.Application.AuthDomainException).</summary>
    public string ErrorType { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    /// <summary>Store DB đang chạy khi lỗi (gắn bởi DbHelper) — rỗng nếu lỗi không đến từ DB.</summary>
    public string StoreName { get; set; } = string.Empty;
    /// <summary>Chỉ ghi khi lỗi hệ thống (HTTP ≥ 500) — lỗi nghiệp vụ 4xx không cần stack trace.</summary>
    public string StackTrace { get; set; } = string.Empty;
    public string RequestBody { get; set; } = string.Empty;
    public string ResponseBody { get; set; } = string.Empty;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    /// <summary>Mili giây — giữ kiểu số để lọc/sort (VD GetSlowLogs).</summary>
    public long ExecutionMs { get; set; }
    /// <summary>Dạng dễ đọc của ExecutionMs: "707 ms", "2.35 s", "1.20 min".</summary>
    public string ExecutionTimeDisplay { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
