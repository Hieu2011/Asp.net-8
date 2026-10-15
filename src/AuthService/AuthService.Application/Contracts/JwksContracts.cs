namespace AuthService.Application.Contracts
{
    /// <summary>JSON Web Key Set (RFC 7517) — public key để bên nhận (ApiCore8) tự verify JWT, chọn khóa theo <c>kid</c>.</summary>
    public class JwksResponse
    {
        public List<JwkDto> Keys { get; set; } = [];
    }

    /// <summary>1 public key RSA dạng JWK — tên field theo chuẩn (kty/use/alg/kid/n/e).</summary>
    public class JwkDto
    {
        public string Kty { get; set; } = "RSA";
        public string Use { get; set; } = "sig";
        public string Alg { get; set; } = "RS256";
        public string Kid { get; set; } = string.Empty;
        public string N { get; set; } = string.Empty;
        public string E { get; set; } = string.Empty;
    }
}
