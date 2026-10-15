using AuthService.Application.Contracts;

namespace AuthService.Application.Interfaces
{
    /// <summary>Redis-backed — trạng thái phiên đăng nhập QR (AUTH-10). Mọi chuyển trạng thái đều nguyên tử.</summary>
    public interface IQrLoginStore
    {
        Task<(string ChallengeId, string PollSecret)> CreateAsync(string ipAddress, string? deviceInfo, CancellationToken cancellationToken = default);

        /// <summary>Đánh dấu đã quét (nếu đang chờ) và trả thông tin máy xin đăng nhập; null nếu không còn.</summary>
        Task<QrChallengeInfo?> ScanAsync(string challengeId, CancellationToken cancellationToken = default);

        /// <summary>Duyệt/từ chối — chỉ thành công khi đang pending/scanned. false = mã không còn hoặc đã có quyết định.</summary>
        Task<bool> DecideAsync(string challengeId, bool isApproved, Guid userId, CancellationToken cancellationToken = default);

        /// <summary>Máy tính hỏi trạng thái. approved/rejected → xóa mã (dùng 1 lần) và trả userId nếu approved.</summary>
        Task<(string Status, Guid? UserId)> PollAsync(string challengeId, string pollSecret, CancellationToken cancellationToken = default);
    }
}
