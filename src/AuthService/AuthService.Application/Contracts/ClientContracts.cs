namespace AuthService.Application.Contracts
{
    /// <summary>
    /// PLAN.md mục 22/7 — POST /auth/token (Flow B). client_id/client_secret đi qua header
    /// Authorization: Basic (không nằm trong body) — request chỉ còn grant_type để xác nhận đúng
    /// chuẩn OAuth2 Client Credentials Grant (RFC 6749).
    /// </summary>
    public class ClientTokenRequest
    {
        public string GrantType { get; set; } = "client_credentials";
    }

    public class ClientTokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
    }

    /// <summary>PUT /clients/{id}/regenerate-secret — secret cũ vô hiệu ngay, không grace period (mục 25).</summary>
    public class RegenerateClientSecretResponse
    {
        public string ClientSecret { get; set; } = string.Empty;
    }
}
