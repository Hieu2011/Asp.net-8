namespace AuthService.Application.Interfaces
{
    /// <summary>TOTP — Otp.NET (PLAN.md mục 3). Implement ở Infrastructure.</summary>
    public interface IOtpService
    {
        /// <summary>Sinh secret ngẫu nhiên mới (base32), dùng lúc đăng ký/reset/hoàn tất setup Google.</summary>
        string GenerateSecret();

        /// <summary>Dựng chuỗi otpauth://totp/... để user quét QR/nhập tay (mục 2 quyết định).</summary>
        string BuildOtpauthUri(string secret, string accountName);

        /// <summary>
        /// Verify mã 6 số. <paramref name="matchedTimeStep"/> là chu kỳ 30s đã khớp — dùng để chống
        /// replay (mục 30): so với last_used_otp_step, step trùng lần trước thì từ chối dù mã đúng.
        /// </summary>
        bool TryVerify(string secret, string code, out long matchedTimeStep);
    }
}
