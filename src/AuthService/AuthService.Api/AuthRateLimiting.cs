using System.Threading.RateLimiting;
using AuthService.Application;
using AuthService.Application.Interfaces;
using Microsoft.AspNetCore.RateLimiting;
using Shared.Common.Contracts;

namespace AuthService.Api
{
    /// <summary>
    /// Rate limit theo IP thật (built-in .NET 8, in-memory — đủ cho 1 instance) cho các endpoint ẩn danh dễ bị dò/spam.
    /// Gắn bằng <c>[EnableRateLimiting(AuthRateLimiting.Xxx)]</c>; giới hạn theo email nằm ở <see cref="ILoginEmailRateLimiter"/>.
    /// </summary>
    public static class AuthRateLimiting
    {
        public const string Login = "login";
        public const string Register = "register";
        public const string OtpVerify = "otp-verify";
        public const string Forgot = "forgot";
        public const string Refresh = "refresh";
        public const string Google = "google";
        public const string QrStart = "qr-start";
        public const string QrPoll = "qr-poll";

        public static IServiceCollection AddAuthRateLimiter(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.OnRejected = async (context, cancellationToken) =>
                {
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var tspRetryAfter))
                        context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(tspRetryAfter.TotalSeconds)).ToString();
                    await context.HttpContext.Response.WriteAsJsonAsync(
                        APIResult.Error(AuthErrorCode.TooManyRequests, "Bạn thao tác quá nhanh, vui lòng thử lại sau."), cancellationToken);
                };

                AddFixedWindowPerIp(options, Login, 10, TimeSpan.FromMinutes(1));
                AddFixedWindowPerIp(options, Register, 5, TimeSpan.FromHours(1));
                AddFixedWindowPerIp(options, OtpVerify, 10, TimeSpan.FromMinutes(1));
                AddFixedWindowPerIp(options, Forgot, 3, TimeSpan.FromHours(1));
                AddFixedWindowPerIp(options, Refresh, 30, TimeSpan.FromMinutes(1));
                AddFixedWindowPerIp(options, Google, 10, TimeSpan.FromMinutes(1));
                AddFixedWindowPerIp(options, QrStart, 10, TimeSpan.FromMinutes(1));
                AddFixedWindowPerIp(options, QrPoll, 60, TimeSpan.FromMinutes(1));
            });
            return services;
        }

        private static void AddFixedWindowPerIp(RateLimiterOptions options, string strPolicy, int intPermit, TimeSpan tspWindow)
            => options.AddPolicy(strPolicy, httpContext => RateLimitPartition.GetFixedWindowLimiter(
                ResolveClientIp(httpContext),
                _ => new FixedWindowRateLimiterOptions { PermitLimit = intPermit, Window = tspWindow, QueueLimit = 0 }));

        // Cùng logic IP với controller — chỉ tin X-Forwarded-For/CF-Connecting-IP khi đến từ Cloudflare/TrustedProxies
        private static string ResolveClientIp(HttpContext httpContext)
            => httpContext.RequestServices.GetRequiredService<IClientIpResolver>().Resolve(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                httpContext.Request.Headers["CF-Connecting-IP"].FirstOrDefault(),
                httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault());
    }
}
