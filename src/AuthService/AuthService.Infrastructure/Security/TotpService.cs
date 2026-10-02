using AuthService.Application.Interfaces;
using OtpNet;

namespace AuthService.Infrastructure.Security
{
    /// <inheritdoc cref="IOtpService" />
    public class TotpService : IOtpService
    {
        private const string Issuer = "AuthService";

        public string GenerateSecret()
        {
            var key = KeyGeneration.GenerateRandomKey(20); // 160-bit, chuẩn Google Authenticator
            return Base32Encoding.ToString(key);
        }

        public string BuildOtpauthUri(string secret, string accountName)
        {
            var label = Uri.EscapeDataString($"{Issuer}:{accountName}");
            var issuerParam = Uri.EscapeDataString(Issuer);
            return $"otpauth://totp/{label}?secret={secret}&issuer={issuerParam}";
        }

        public bool TryVerify(string secret, string code, out long matchedTimeStep)
        {
            matchedTimeStep = 0;
            if (string.IsNullOrWhiteSpace(code))
                return false;

            try
            {
                var keyBytes = Base32Encoding.ToBytes(secret);
                var totp = new Totp(keyBytes);
                return totp.VerifyTotp(code, out matchedTimeStep, VerificationWindow.RfcSpecifiedNetworkDelay);
            }
            catch (Exception)
            {
                // Secret hỏng/code không phải số... — coi như sai mã, không để lộ lỗi kỹ thuật.
                return false;
            }
        }
    }
}
