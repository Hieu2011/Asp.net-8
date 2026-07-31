using Core;
using ML;
using Microsoft.AspNetCore.Http;
using Serilog;
using System.Collections.Concurrent;

namespace ApiCore8.Midleware
{
    public class AntiSpamMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AntiSpamMiddleware> _logger;
        
        // Thread-safe dictionary lưu tracking info
        private static readonly ConcurrentDictionary<string, RateLimitInfo> _rateLimitStore = new();
        
        // Configuration
        private TimeSpan _detectionWindow = TimeSpan.FromMilliseconds(500); // 0.5 giây
        private int _maxRequestsInWindow = 1; // Tối đa 1 request trong 0.5s
        private TimeSpan _blockDuration = TimeSpan.FromMinutes(1); // Block 1 phút
        
        // Cleanup timer
        private static readonly Timer _cleanupTimer;
        
        static AntiSpamMiddleware()
        {
            // Cleanup mỗi 5 phút để tránh memory leak
            _cleanupTimer = new Timer(
                callback: _ => CleanupExpiredEntries(),
                state: null,
                dueTime: TimeSpan.FromMinutes(5),
                period: TimeSpan.FromMinutes(5)
            );
        }

        public AntiSpamMiddleware(RequestDelegate next, ILogger<AntiSpamMiddleware> logger, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            
            // ✅ Đọc từ config
            _detectionWindow = TimeSpan.FromMilliseconds(
                configuration.GetValue<int>("AntiSpam:DetectionWindowMilliseconds", 500)
            );
            _maxRequestsInWindow = configuration.GetValue<int>("AntiSpam:MaxRequestsInWindow", 1);
            _blockDuration = TimeSpan.FromMinutes(
                configuration.GetValue<int>("AntiSpam:BlockDurationMinutes", 1)
            );
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Lấy client IP
            var clientIP = GetClientIP(context);
            var endpoint = $"{context.Request.Method} {context.Request.Path}";
            var key = $"{clientIP}:{endpoint}";

            // Bỏ qua các endpoint đặc biệt
            if (ShouldSkipRateLimit(context))
            {
                await _next(context);
                return;
            }

            // Lấy hoặc tạo tracking info
            var rateLimitInfo = _rateLimitStore.GetOrAdd(key, _ => new RateLimitInfo
            {
                ClientIP = clientIP,
                Endpoint = endpoint
            });

            lock (rateLimitInfo)
            {
                // Kiểm tra xem IP có đang bị block không
                if (rateLimitInfo.IsBlocked)
                {
                    var remainingSeconds = (int)(rateLimitInfo.BlockedUntil!.Value - DateTime.Now).TotalSeconds;
                    
                    _logger.LogWarning(
                        "Blocked spam request from IP: {IP} to {Endpoint}. Remaining block time: {Seconds}s",
                        clientIP, endpoint, remainingSeconds
                    );

                    ReturnBlockedResponse(context, remainingSeconds, rateLimitInfo.SpamCount);
                    return;
                }

                // Cleanup các request cũ ngoài time window
                rateLimitInfo.CleanupOldRequests(_detectionWindow);

                // Kiểm tra số lượng request trong time window
                var requestCount = rateLimitInfo.RequestTimestamps.Count;

                if (requestCount >= _maxRequestsInWindow)
                {
                    // SPAM DETECTED! Block IP
                    rateLimitInfo.Block(_blockDuration);
                    
                    _logger.LogWarning(
                        "SPAM DETECTED! IP: {IP} blocked for {Minutes} minute(s). " +
                        "Endpoint: {Endpoint}, Requests in {Seconds}s: {Count}, Total spam count: {SpamCount}",
                        clientIP, _blockDuration.TotalMinutes, endpoint, 
                        _detectionWindow.TotalSeconds, requestCount + 1, rateLimitInfo.SpamCount
                    );

                    ReturnSpamDetectedResponse(context, rateLimitInfo.SpamCount);
                    return;
                }

                // Thêm request hiện tại vào tracking
                rateLimitInfo.AddRequest();
            }

            // Cho phép request đi tiếp
            await _next(context);
        }

        private string GetClientIP(HttpContext context)
        {
            // Ưu tiên lấy IP từ header (nếu đằng sau proxy/load balancer)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            var realIP = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIP))
            {
                return realIP;
            }

            // Fallback to connection IP
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private bool ShouldSkipRateLimit(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";
            
            // Bỏ qua các endpoint này
            return path.Contains("/health") 
                || path.Contains("/swagger") 
                || path.Contains("/favicon.ico");
        }

        private void ReturnSpamDetectedResponse(HttpContext context, int spamCount)
        {
            // ✅ Check if response has already started
            if (context.Response.HasStarted)
            {
                _logger.LogWarning("Cannot write spam response - headers already sent");
                return;
            }

            try
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.ContentType = "application/json";
                context.Response.Headers["Retry-After"] = ((int)_blockDuration.TotalSeconds).ToString();

                var response = new APIResult(
                    true,
                    ResultMessage.ErrorTypes.RateLimit,
                    "⚠️ API đang bị SPAM!",
                    $"Hệ thống phát hiện spam từ IP của bạn. " +
                    $"IP đã bị chặn trong {_blockDuration.TotalMinutes} phút. " +
                    $"Số lần vi phạm: {spamCount}. " +
                    $"Vui lòng không gửi request liên tục trong vòng {_detectionWindow.TotalSeconds} giây."
                );

                // ✅ Synchronous write (middleware context)
                context.Response.WriteAsJsonAsync(response).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing spam detected response");
            }
        }

        private void ReturnBlockedResponse(HttpContext context, int remainingSeconds, int spamCount)
        {
            // ✅ Check if response has already started
            if (context.Response.HasStarted)
            {
                _logger.LogWarning("Cannot write blocked response - headers already sent");
                return;
            }

            try
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.ContentType = "application/json";
                context.Response.Headers["Retry-After"] = remainingSeconds.ToString();

                var response = new APIResult(
                    true,
                    ResultMessage.ErrorTypes.RateLimit,
                    "🚫 IP của bạn đang bị chặn",
                    $"IP của bạn đã bị chặn do spam. " +
                    $"Thời gian còn lại: {remainingSeconds} giây. " +
                    $"Tổng số lần vi phạm: {spamCount}. " +
                    $"Vui lòng chờ {remainingSeconds}s trước khi thử lại."
                );

                // ✅ Synchronous write (middleware context)
                context.Response.WriteAsJsonAsync(response).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing blocked response");
            }
        }

        private static void CleanupExpiredEntries()
        {
            var now = DateTime.Now;
            var keysToRemove = new List<string>();

            foreach (var kvp in _rateLimitStore)
            {
                var info = kvp.Value;
                
                // Xóa entry nếu:
                // 1. Không bị block VÀ không có request nào trong 10 phút
                // 2. Bị block nhưng thời gian block đã qua 5 phút
                if ((!info.IsBlocked && !info.RequestTimestamps.Any(t => t > now - TimeSpan.FromMinutes(10)))
                    || (info.BlockedUntil.HasValue && info.BlockedUntil.Value < now - TimeSpan.FromMinutes(5)))
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _rateLimitStore.TryRemove(key, out _);
            }

            if (keysToRemove.Count > 0)
            {
                Log.Information("Anti-spam cleanup: Removed {Count} expired entries", keysToRemove.Count);
            }
        }
    }
}