using System.Net;
using System.Net.Sockets;
using AuthService.Application.Interfaces;

namespace AuthService.Infrastructure.Security
{
    /// <inheritdoc cref="IClientIpResolver" />
    /// <remarks>
    /// Dải IP Cloudflare (mục 24) nhúng tĩnh — lấy từ cloudflare.com/ips, cần CẬP NHẬT THỦ CÔNG
    /// định kỳ vì Cloudflare thỉnh thoảng đổi (không có API tự động ở đây để giữ đơn giản).
    /// </remarks>
    public class ClientIpResolver : IClientIpResolver
    {
        private static readonly string[] CloudflareCidrRanges =
        [
            // IPv4
            "173.245.48.0/20", "103.21.244.0/22", "103.22.200.0/22", "103.31.4.0/22",
            "141.101.64.0/18", "108.162.192.0/18", "190.93.240.0/20", "188.114.96.0/20",
            "197.234.240.0/22", "198.41.128.0/17", "162.158.0.0/15", "104.16.0.0/13",
            "104.24.0.0/14", "172.64.0.0/13", "131.0.72.0/22",
            // IPv6
            "2400:cb00::/32", "2606:4700::/32", "2803:f800::/32", "2405:b500::/32",
            "2405:8100::/32", "2a06:98c0::/29", "2c0f:f248::/32"
        ];

        public string Resolve(string remoteIpAddress, string? cfConnectingIp, string? xForwardedFor)
        {
            if (!IsFromCloudflare(remoteIpAddress))
                return remoteIpAddress;

            if (!string.IsNullOrWhiteSpace(cfConnectingIp))
                return cfConnectingIp.Trim();

            if (!string.IsNullOrWhiteSpace(xForwardedFor))
            {
                // X-Forwarded-For có thể là "client, proxy1, proxy2" — IP đầu tiên là client thật.
                var first = xForwardedFor.Split(',')[0].Trim();
                if (!string.IsNullOrEmpty(first))
                    return first;
            }

            return remoteIpAddress;
        }

        private static bool IsFromCloudflare(string remoteIpAddress)
        {
            if (!IPAddress.TryParse(remoteIpAddress, out var address))
                return false;

            return CloudflareCidrRanges.Any(cidr => IsInCidrRange(address, cidr));
        }

        private static bool IsInCidrRange(IPAddress address, string cidr)
        {
            var parts = cidr.Split('/');
            if (!IPAddress.TryParse(parts[0], out var network))
                return false;

            var prefixLength = parts.Length > 1
                ? int.Parse(parts[1])
                : (network.AddressFamily == AddressFamily.InterNetwork ? 32 : 128);

            var addressBytes = address.GetAddressBytes();
            var networkBytes = network.GetAddressBytes();
            if (addressBytes.Length != networkBytes.Length)
                return false;

            var fullBytes = prefixLength / 8;
            var remainingBits = prefixLength % 8;

            for (var i = 0; i < fullBytes; i++)
            {
                if (addressBytes[i] != networkBytes[i])
                    return false;
            }

            if (remainingBits > 0)
            {
                var mask = (byte)~(0xFF >> remainingBits);
                if ((addressBytes[fullBytes] & mask) != (networkBytes[fullBytes] & mask))
                    return false;
            }

            return true;
        }
    }
}
