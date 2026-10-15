using AuthService.Application.Contracts;
using AuthService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Shared.Common.Contracts;

namespace AuthService.Api.Controllers
{
    /// <summary>Đăng nhập bằng QR (AUTH-10): start/poll cho máy tính chưa đăng nhập, scan/approve/reject cho điện thoại đã đăng nhập.</summary>
    [Route("auth/qr")]
    public sealed class QrLoginController : AuthApiControllerBase
    {
        private readonly IQrLoginService _qrLoginService;

        public QrLoginController(IQrLoginService qrLoginService, IClientIpResolver clientIpResolver)
            : base(clientIpResolver)
        {
            _qrLoginService = qrLoginService;
        }

        [EnableRateLimiting(AuthRateLimiting.QrStart)]
        [HttpPost("start")]
        public async Task<APIResult> Start(CancellationToken cancellationToken)
            => new(await _qrLoginService.StartAsync(RemoteIp, DeviceInfo, cancellationToken));

        [EnableRateLimiting(AuthRateLimiting.QrPoll)]
        [HttpPost("poll")]
        public async Task<APIResult> Poll([FromBody] QrPollRequest request, CancellationToken cancellationToken)
            => new(await _qrLoginService.PollAsync(request, RemoteIp, DeviceInfo, cancellationToken));

        [Authorize]
        [HttpPost("{challengeId}/scan")]
        public async Task<APIResult> Scan(string challengeId, CancellationToken cancellationToken)
            => new(await _qrLoginService.ScanAsync(challengeId, cancellationToken));

        [Authorize]
        [HttpPost("{challengeId}/approve")]
        public async Task<APIResult> Approve(string challengeId, CancellationToken cancellationToken)
        {
            await _qrLoginService.ApproveAsync(challengeId, CurrentUserId, cancellationToken);
            return new APIResult(new { Success = true });
        }

        [Authorize]
        [HttpPost("{challengeId}/reject")]
        public async Task<APIResult> Reject(string challengeId, CancellationToken cancellationToken)
        {
            await _qrLoginService.RejectAsync(challengeId, CurrentUserId, cancellationToken);
            return new APIResult(new { Success = true });
        }
    }
}
