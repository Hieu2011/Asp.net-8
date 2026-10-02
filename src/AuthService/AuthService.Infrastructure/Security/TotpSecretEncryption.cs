using AuthService.Application.Interfaces;
using AuthService.Application.Options;
using Microsoft.Extensions.Options;
using Shared.Common.Security;

namespace AuthService.Infrastructure.Security
{
    /// <summary>
    /// Mã hóa TOTP secret bằng <see cref="AesGzipCipher"/> (Shared.DataAccess) — thuật toán dùng
    /// chung với ApiCore8.RedisConnectionService (GZip compress → AES-CBC, key = SHA256(secret),
    /// IV ngẫu nhiên prepend, Base64), tránh 2 bản code trùng nhau dễ lệch khi sửa 1 bên quên bên kia.
    /// </summary>
    public class TotpSecretEncryption : ITotpSecretEncryption
    {
        private readonly AesGzipCipher _cipher;

        public TotpSecretEncryption(IOptions<AuthOptions> options)
        {
            _cipher = new AesGzipCipher(options.Value.TotpEncryptionKey);
        }

        public string Encrypt(string plainSecret) => _cipher.CompressAndEncrypt(plainSecret);

        public string Decrypt(string encryptedSecret) => _cipher.DecompressAndDecrypt(encryptedSecret);
    }
}
