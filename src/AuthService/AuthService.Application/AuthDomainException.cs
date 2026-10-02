using Shared.Common.Contracts;
namespace AuthService.Application
{
    /// <summary>
    /// Ném ra khi 1 rule nghiệp vụ vi phạm (khóa tài khoản, sai OTP, gate không đủ điều kiện...).
    /// Controller (Bước 3) bắt exception này, map <see cref="ErrorCode"/> vào APIResult.StatusID —
    /// Service không tự build APIResult (Application không nên biết khái niệm response HTTP cụ thể).
    /// </summary>
    public class AuthDomainException : Exception
    {
        public AuthErrorCode ErrorCode { get; }

        /// <summary>Thời gian còn lại (nếu là lỗi khóa tạm) — client hiện đếm ngược (mục 40).</summary>
        public TimeSpan? RetryAfter { get; }

        public AuthDomainException(AuthErrorCode errorCode, string message, TimeSpan? retryAfter = null)
            : base(message)
        {
            ErrorCode = errorCode;
            RetryAfter = retryAfter;
        }
    }
}
