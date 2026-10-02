namespace AuthService.Application.Interfaces
{
    /// <summary>
    /// Redis-backed — quản lý toàn bộ đếm sai/khóa OTP + Password fallback (mục 17/18/29/30/35).
    /// Implement ở Infrastructure (OtpFailCounterService).
    /// </summary>
    public interface IOtpLockoutService
    {
        // --- OTP (mục 17) ---
        /// <summary>Tăng otp_fail_count. Đạt 3 → tự set otp_locked_until + reset counter. Trả về count mới.</summary>
        Task<int> IncrementOtpFailAsync(Guid userId, CancellationToken cancellationToken = default);
        Task ResetOtpFailAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<int> GetOtpFailCountAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<bool> IsOtpLockedAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<TimeSpan?> GetOtpLockRemainingAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>Chống replay cùng chu kỳ 30s (mục 30) — true nếu step MỚI (chưa dùng), false nếu trùng lần trước.</summary>
        Task<bool> TryRecordOtpStepAsync(Guid userId, long timeStep, CancellationToken cancellationToken = default);

        // --- Password fallback (mục 18) ---
        /// <summary>Tăng password_fail_count. Đạt ngưỡng → set account_locked_until + tăng account_lockout_count.
        /// Trả về (failCount mới, lockoutEscalationCount mới) để caller (Service) tự quyết định leo thang vĩnh viễn (mục 35).</summary>
        Task<(int FailCount, int LockoutEscalationCount)> IncrementPasswordFailAsync(Guid userId, CancellationToken cancellationToken = default);
        Task ResetPasswordFailAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<bool> IsAccountLockedAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<TimeSpan?> GetAccountLockRemainingAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>Admin unlock (mục 19/37) hoặc tự phục hồi qua email (mục 36) — xóa mọi key lock/counter.</summary>
        Task ClearAllLocksAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
