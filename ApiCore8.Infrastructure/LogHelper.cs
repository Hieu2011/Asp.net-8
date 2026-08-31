using System.Net.Sockets;
using System.Net;

namespace ApiCore8.Infrastructure
{
    public static class LogHelper
    {
        public static string GetClientIp()
        {
            try
            {
                string hostName = Dns.GetHostName();
                var ipEntry = Dns.GetHostEntry(hostName);
                var ipAddress = ipEntry.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                return ipAddress?.ToString() ?? "Unknown IP";
            }
            catch
            {
                return "127.0.0.1";
            }
        }
    }
}
