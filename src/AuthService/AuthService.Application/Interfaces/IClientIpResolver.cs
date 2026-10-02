namespace AuthService.Application.Interfaces
{
    /// <summary>
    /// Lấy IP client thật (mục 24). Nhận thẳng string thay vì HttpContext — Application không phụ
    /// thuộc ASP.NET Core hosting model, tầng Api tự đọc header/RemoteIpAddress rồi truyền vào.
    /// </summary>
    public interface IClientIpResolver
    {
        /// <summary>
        /// Ưu tiên cfConnectingIp → xForwardedFor → remoteIpAddress. Chỉ tin header khi
        /// remoteIpAddress nằm trong dải IP chính thức của Cloudflare, ngược lại dùng thẳng remoteIpAddress.
        /// </summary>
        string Resolve(string remoteIpAddress, string? cfConnectingIp, string? xForwardedFor);
    }
}
