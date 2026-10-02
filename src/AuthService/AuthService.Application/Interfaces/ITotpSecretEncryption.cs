namespace AuthService.Application.Interfaces
{
    /// <summary>
    /// AES, key từ AuthOptions.TotpEncryptionKey (mục 13) — tái dùng pattern
    /// RedisConnectionService.CompressAndEncrypt/DecompressAndDecrypt (SHA256-derive key, IV prepend, base64).
    /// </summary>
    public interface ITotpSecretEncryption
    {
        string Encrypt(string plainSecret);
        string Decrypt(string encryptedSecret);
    }
}
