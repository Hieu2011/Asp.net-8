using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Common.Contracts;
using Shared.Common.Exceptions;

namespace Shared.Common.Middlewares;

/// <summary>Bắt mọi exception chưa xử lý → log 1 lần + trả APIResult JSON (không lộ stack trace ra client).</summary>
public sealed class GlobalExceptionMiddleware
{
    public const int UnhandledErrorStatusID = StatusCodes.Status500InternalServerError;

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client tự hủy request (đóng tab, timeout phía client) — không phải lỗi hệ thống
        }
        catch (Exception objException)
        {
            // Đã gửi header/body thì không ghi đè response được nữa → để ASP.NET Core tự xử lý
            if (context.Response.HasStarted) throw;

            // Cùng TraceId W3C mà Serilog lưu ở SystemLogs và [LogApi] lưu ở ApiLogs → tra chéo được
            string strTraceID = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
            string strStoreName = objException.GetStoreName();
            if (strStoreName.Length > 0)
                _logger.LogError(objException, "Lỗi chưa xử lý {Method} {Path} tại store {StoreName} (traceId {TraceId})",
                    context.Request.Method, context.Request.Path, strStoreName, strTraceID);
            else
                _logger.LogError(objException, "Lỗi chưa xử lý {Method} {Path} (traceId {TraceId})",
                    context.Request.Method, context.Request.Path, strTraceID);

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            var objResult = new APIResult(true, UnhandledErrorStatusID,
                $"Hệ thống đang gặp sự cố, vui lòng thử lại sau (mã lỗi: {strTraceID})", string.Empty);
            await context.Response.WriteAsJsonAsync(objResult, context.RequestAborted);
        }
    }
}

public static class GlobalExceptionMiddlewareExtensions
{
    /// <summary>Đặt đầu pipeline để bắt được lỗi của mọi middleware phía sau.</summary>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        => app.UseMiddleware<GlobalExceptionMiddleware>();
}
