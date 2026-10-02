using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace Shared.Common.Security
{
    /// <summary>
    /// GZip compress + AES-CBC encrypt (key = SHA256(secret), IV ngẫu nhiên 16 byte prepend vào
    /// ciphertext, Base64 output) — dùng chung giữa ApiCore8 (<c>RedisConnectionService</c>) và
    /// AuthService (<c>TotpSecretEncryption</c>), tránh 2 bản thuật toán trùng nhau dễ lệch khi
    /// sửa 1 bên quên sửa bên kia.
    /// </summary>
    public class AesGzipCipher
    {
        private readonly byte[] _aesKey;

        public AesGzipCipher(string secret)
        {
            using var sha256 = SHA256.Create();
            _aesKey = sha256.ComputeHash(Encoding.UTF8.GetBytes(secret));
        }

        public string CompressAndEncrypt(string text, CompressionLevel compressionLevel = CompressionLevel.Optimal)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var buffer = Encoding.UTF8.GetBytes(text);
            byte[] compressedData;
            using (var memoryStream = new MemoryStream())
            {
                using (var gZipStream = new GZipStream(memoryStream, compressionLevel))
                {
                    gZipStream.Write(buffer, 0, buffer.Length);
                }
                compressedData = memoryStream.ToArray();
            }

            using var aes = Aes.Create();
            aes.Key = _aesKey;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var encryptedStream = new MemoryStream();
            encryptedStream.Write(aes.IV, 0, aes.IV.Length);

            using (var cryptoStream = new CryptoStream(encryptedStream, encryptor, CryptoStreamMode.Write))
            {
                cryptoStream.Write(compressedData, 0, compressedData.Length);
            }

            return Convert.ToBase64String(encryptedStream.ToArray());
        }

        public async Task<string> CompressAndEncryptAsync(string text, CompressionLevel compressionLevel = CompressionLevel.Optimal)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var buffer = Encoding.UTF8.GetBytes(text);
            byte[] compressedData;
            using (var memoryStream = new MemoryStream())
            {
                using (var gZipStream = new GZipStream(memoryStream, compressionLevel))
                {
                    await gZipStream.WriteAsync(buffer);
                }
                compressedData = memoryStream.ToArray();
            }

            using var aes = Aes.Create();
            aes.Key = _aesKey;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var encryptedStream = new MemoryStream();
            await encryptedStream.WriteAsync(aes.IV);

            using (var cryptoStream = new CryptoStream(encryptedStream, encryptor, CryptoStreamMode.Write))
            {
                await cryptoStream.WriteAsync(compressedData);
            }

            return Convert.ToBase64String(encryptedStream.ToArray());
        }

        public string DecompressAndDecrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return string.Empty;

            var fullCipher = Convert.FromBase64String(encryptedText);
            if (fullCipher.Length < 16)
                throw new InvalidOperationException($"Encrypted data too short: {fullCipher.Length} bytes");

            byte[] decryptedData;
            using (var aes = Aes.Create())
            {
                aes.Key = _aesKey;

                var iv = new byte[16];
                Array.Copy(fullCipher, 0, iv, 0, 16);
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var decryptedStream = new MemoryStream();
                using (var cryptoStream = new CryptoStream(decryptedStream, decryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(fullCipher, 16, fullCipher.Length - 16);
                }
                decryptedData = decryptedStream.ToArray();
            }

            using var memoryStream = new MemoryStream(decryptedData);
            using var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress);
            using var resultStream = new MemoryStream();
            gZipStream.CopyTo(resultStream);
            return Encoding.UTF8.GetString(resultStream.ToArray());
        }

        public async Task<string> DecompressAndDecryptAsync(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return string.Empty;

            var fullCipher = Convert.FromBase64String(encryptedText);
            if (fullCipher.Length < 16)
                throw new InvalidOperationException($"Encrypted data too short: {fullCipher.Length} bytes");

            byte[] decryptedData;
            using (var aes = Aes.Create())
            {
                aes.Key = _aesKey;

                var iv = new byte[16];
                Array.Copy(fullCipher, 0, iv, 0, 16);
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var decryptedStream = new MemoryStream();
                using (var cryptoStream = new CryptoStream(decryptedStream, decryptor, CryptoStreamMode.Write))
                {
                    await cryptoStream.WriteAsync(fullCipher.AsMemory(16, fullCipher.Length - 16));
                }
                decryptedData = decryptedStream.ToArray();
            }

            using var memoryStream = new MemoryStream(decryptedData);
            using var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress);
            using var resultStream = new MemoryStream();
            await gZipStream.CopyToAsync(resultStream);
            return Encoding.UTF8.GetString(resultStream.ToArray());
        }
    }
}
