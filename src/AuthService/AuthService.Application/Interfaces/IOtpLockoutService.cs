namespace AuthService.Application.Interfaces
{
    /// <summary>
    /// Redis-backed — quản lý toàn bộ đếm sai/khóa OTP + Password fallback (mục 17/18/29/30/35).
    /// Implement ở Infrastructure (OtpFailCounterService).
    /// </summary>
    public interface IOtpLockoutService
    {
        // --- OTP (mục 17) ---
        /// <summary>Giữ chỗ 1 lượt thử OTP TRƯỚC khi verify (nguyên tử — chặn dò song song). Trả số thứ tự lượt (1..3);
        /// null nếu OTP đang khóa hoặc đã đủ 3 lượt đang xử lý.</summary>
        Task<int?> TryReserveOtpAttemptAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>Trả lại lượt đã giữ khi request bị từ chối vì lý do khác sai mã (thiếu captcha, replay).</summary>
        Task ReleaseOtpAttemptAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>Lượt thử sai mã — lượt thứ 3 thì set otp_locked_until + reset counter (mục 17).</summary>
        Task RecordOtpFailureAsync(Guid userId, int attemptNumber, CancellationToken cancellationToken = default);
        Task ResetOtpFailAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<bool> IsOtpLockedAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<TimeSpan?> GetOtpLockRemainingAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>Chống replay (mục 30) — true nếu step MỚI HƠN step đã dùng, false nếu trùng hoặc cũ hơn.</summary>
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
