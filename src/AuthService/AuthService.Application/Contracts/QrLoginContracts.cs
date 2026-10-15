namespace AuthService.Application.Contracts
{
    /// <summary>POST /auth/qr/start — máy tính (chưa đăng nhập) xin mã QR (AUTH-10).</summary>
    public class QrStartResponse
    {
        /// <summary>In vào QR — điện thoại quét được nên không coi là bí mật.</summary>
        public string ChallengeId { get; set; } = string.Empty;
        /// <summary>Chỉ máy tính giữ — người chụp trộm QR không có nên không lấy được token.</summary>
        public string PollSecret { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public int PollInterval { get; set; }
    }

    /// <summary>POST /auth/qr/poll.</summary>
    public class QrPollRequest
    {
        public string ChallengeId { get; set; } = string.Empty;
        public string PollSecret { get; set; } = string.Empty;
    }

    public static class QrLoginStatus
    {
        public const string Pending = "pending";
        public const string Scanned = "scanned";
        public const string Approved = "approved";
        public const string Rejected = "rejected";
        public const string Expired = "expired";
    }

    /// <summary>Status theo <see cref="QrLoginStatus"/>; Token chỉ có khi approved (cấp đúng 1 lần).</summary>
    public class QrPollResponse
    {
        public string Status { get; set; } = QrLoginStatus.Pending;
        public TokenResponse? Token { get; set; }
    }

    /// <summary>Thông tin máy đang xin đăng nhập — hiện trên điện thoại để người dùng xác nhận.</summary>
    public class QrChallengeInfo
    {
        public string Status { get; set; } = QrLoginStatus.Pending;
        public string? DeviceInfo { get; set; }
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
