using AuthService.Application.Interfaces;

namespace AuthService.Infrastructure.Security
{
    /// <inheritdoc cref="IPasswordHasher" />
    public class BCryptPasswordHasher : IPasswordHasher
    {
        public string Hash(string plainText) => BCrypt.Net.BCrypt.HashPassword(plainText);

        public bool Verify(string plainText, string hash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(plainText, hash);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                // Hash lưu trong DB bị hỏng/không đúng định dạng BCrypt — coi như không khớp,
                // không ném lỗi ra ngoài làm lộ chi tiết kỹ thuật cho client.
                return false;
            }
        }
    }
}
