using AuthService.Application.Contracts;

namespace AuthService.Application.Interfaces
{
    /// <summary>Đăng nhập bằng QR (AUTH-10): máy tính hiện mã, điện thoại đã đăng nhập quét + duyệt.</summary>
    public interface IQrLoginService
    {
        Task<QrStartResponse> StartAsync(string remoteIp, string? deviceInfo, CancellationToken cancellationToken = default);

        /// <summary>Điện thoại mở link trong QR — đánh dấu đã quét, trả thông tin máy đang xin đăng nhập.</summary>
        Task<QrChallengeInfo> ScanAsync(string challengeId, CancellationToken cancellationToken = default);

        Task ApproveAsync(string challengeId, Guid userId, CancellationToken cancellationToken = default);

        Task RejectAsync(string challengeId, Guid userId, CancellationToken cancellationToken = default);

        /// <summary>Máy tính hỏi trạng thái; approved → tạo session + token cho máy tính (1 lần).</summary>
        Task<QrPollResponse> PollAsync(QrPollRequest request, string remoteIp, string? deviceInfo, CancellationToken cancellationToken = default);
    }
}
