using AuthService.Domain;

namespace AuthService.Application.Interfaces
{
    /// <summary>Ký JWT bằng RSA, ép RS256 (mục 4/15/16/43). Implement ở Infrastructure.</summary>
    public interface IJwtService
    {
        /// <summary>Access token cho user thường — claim sub=userId, username, role_level, session_id.
        /// Username nằm trong claim để tầng Api lấy ra ghi vào cột updated_user/created_user (mục 4) mà
        /// không cần query lại DB.</summary>
        string IssueAccessToken(Guid userId, string username, RoleLevel roleLevel, Guid sessionId, TimeSpan ttl);

        /// <summary>Access token cho đối tác Flow B — claim client_id, scopes.</summary>
        string IssueClientAccessToken(string clientId, IReadOnlyList<string> scopes, TimeSpan ttl);
    }
}
