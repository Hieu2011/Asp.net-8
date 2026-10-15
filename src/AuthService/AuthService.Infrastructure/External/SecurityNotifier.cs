using System.Security.Cryptography;
using System.Text;
using AuthService.Application.Contracts;
using AuthService.Application.Interfaces;
using AuthService.Domain;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace AuthService.Infrastructure.External
{
    /// <inheritdoc cref="ISecurityNotifier" />
    /// <remarks>Thiết bị đã biết lưu Redis <c>known_devices:{userId}</c> (hash User-Agent, nhớ 90 ngày) — không cần thêm bảng DB.</remarks>
    public sealed class SecurityNotifier : ISecurityNotifier
    {
        private const int MaxDeviceLength = 150;

        private static readonly TimeSpan KnownDeviceTtl = TimeSpan.FromDays(90);
        private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

        private readonly IEmailSender _emailSender;
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<SecurityNotifier> _logger;

        public SecurityNotifier(IEmailSender emailSender, IConnectionMultiplexer redis, ILogger<SecurityNotifier> logger)
        {
            _emailSender = emailSender;
            _redis = redis;
            _logger = logger;
        }

        public async Task NotifyLoginAsync(User user, string remoteIp, string? deviceInfo, string loginMethod, CancellationToken cancellationToken = default)
        {
            try
            {
                var bolNewDevice = await TryRememberDeviceAsync(user.Id, deviceInfo);
                if (loginMethod == LoginMethods.Qr)
                {
                    await SendAsync(user, "Tài khoản vừa đăng nhập bằng mã QR",
                        $"Tài khoản của bạn vừa được đăng nhập bằng mã QR trên {DescribeContext(remoteIp, deviceInfo)}.", cancellationToken);
                }
                else if (bolNewDevice)
                {
                    await SendAsync(user, "Đăng nhập từ thiết bị mới",
                        $"Tài khoản của bạn vừa đăng nhập từ thiết bị chưa từng dùng: {DescribeContext(remoteIp, deviceInfo)}.", cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không gửi được email cảnh báo đăng nhập cho user {UserId}", user.Id);
            }
        }

        public async Task NotifyPasswordResetAsync(User user, string remoteIp, CancellationToken cancellationToken = default)
        {
            try
            {
                await SendAsync(user, "Mật khẩu đã được đặt lại",
                    $"Mật khẩu và mã xác thực (TOTP) của tài khoản vừa được đặt lại từ IP {remoteIp} lúc {FormatNow()}. "
                    + "Mọi thiết bị đã bị đăng xuất.", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không gửi được email báo đặt lại mật khẩu cho user {UserId}", user.Id);
            }
        }

        private Task SendAsync(User user, string strSubject, string strBody, CancellationToken cancellationToken)
            => _emailSender.SendAsync(user.Email, strSubject,
                $"{strBody} Nếu không phải bạn, hãy đổi mật khẩu ngay và đăng xuất khỏi tất cả thiết bị trong trang Tài khoản.",
                cancellationToken);

        // SADD trả true khi phần tử mới → thiết bị lần đầu thấy
        private async Task<bool> TryRememberDeviceAsync(Guid guidUserID, string? strDeviceInfo)
        {
            var objDb = _redis.GetDatabase();
            var strKey = $"known_devices:{guidUserID}";
            var strDeviceHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(strDeviceInfo ?? string.Empty))).ToLowerInvariant();
            var bolAdded = await objDb.SetAddAsync(strKey, strDeviceHash);
            await objDb.KeyExpireAsync(strKey, KnownDeviceTtl);
            return bolAdded;
        }

        private static string DescribeContext(string strRemoteIp, string? strDeviceInfo)
        {
            var strDevice = string.IsNullOrWhiteSpace(strDeviceInfo) ? "thiết bị không rõ"
                : strDeviceInfo.Length > MaxDeviceLength ? strDeviceInfo[..MaxDeviceLength] : strDeviceInfo;
            return $"{strDevice}, IP {strRemoteIp}, lúc {FormatNow()}";
        }

        private static string FormatNow() => $"{DateTime.UtcNow.Add(VietnamOffset):HH:mm dd/MM/yyyy} (giờ Việt Nam)";
    }
}
