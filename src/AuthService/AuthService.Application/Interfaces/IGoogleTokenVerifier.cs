namespace AuthService.Application.Interfaces
{
    /// <summary>Kết quả verify ID Token Google — chỉ giữ 3 field thật sự cần dùng (mục 45).</summary>
    public class GoogleTokenPayload
    {
        /// <summary>Claim "sub" — định danh Google ổn định, lưu vào users.google_id.</summary>
        public string Subject { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool EmailVerified { get; set; }
    }

    /// <summary>Google.Apis.Auth (mục 45). Implement ở Infrastructure.</summary>
    public interface IGoogleTokenVerifier
    {
        /// <summary>Trả null nếu idToken không hợp lệ (sai chữ ký/aud/iss/hết hạn) — không throw ra ngoài.</summary>
        Task<GoogleTokenPayload?> VerifyAsync(string idToken, CancellationToken cancellationToken = default);
    }
}
