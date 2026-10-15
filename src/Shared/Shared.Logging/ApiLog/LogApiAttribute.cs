using System.Diagnostics;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Shared.Common.Exceptions;

namespace Shared.Logging.ApiLog;

/// <summary>Ghi log thực thi API vào Mongo qua hàng đợi (không chặn request) — body đã che field nhạy cảm + cắt chuỗi dài.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class LogApiAttribute : ActionFilterAttribute
{
    private const int MaxStackTraceLength = 4000;

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Attribute là 1 instance dùng chung mọi request → state chỉ để biến local, không dùng field
        DateTime dtmStart = DateTime.UtcNow;
        long lngStartTimestamp = Stopwatch.GetTimestamp();
        string strRequestBody = SerializeArguments(context.ActionArguments);
        ActionExecutedContext objExecuted = await next();
        long lngElapsedMs = (long)Stopwatch.GetElapsedTime(lngStartTimestamp).TotalMilliseconds;
        HttpContext httpContext = context.HttpContext;
        // Swagger hiện response header → xem được thời gian xử lý ngay
        httpContext.Response.Headers["X-Response-Time-Ms"] = lngElapsedMs.ToString();
        Channel<ApiExecutionLog>? objChannel = httpContext.RequestServices.GetService<Channel<ApiExecutionLog>>();
        if (objChannel == null) return;
        Exception? objException = objExecuted.ExceptionHandled ? null : objExecuted.Exception;
        ApiExecutionLog objLog = new()
        {
            ApiName = $"{httpContext.Request.Method} {httpContext.Request.Path}",
            Method = httpContext.Request.Method,
            ClientIP = GetClientIP(httpContext),
            TraceID = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier,
            RequestBody = ApiLogSanitizer.Sanitize(strRequestBody),
            ResponseBody = objException == null ? ApiLogSanitizer.Sanitize(GetResponseBody(objExecuted)) : string.Empty,
            ErrorType = objException?.GetType().FullName ?? string.Empty,
            ErrorMessage = objException?.Message ?? string.Empty,
            StoreName = objException?.GetStoreName() ?? string.Empty,
            StartTime = dtmStart,
            EndTime = DateTime.UtcNow,
            ExecutionMs = lngElapsedMs,
            ExecutionTimeDisplay = FormatExecutionTime(lngElapsedMs)
        };
        // Exception filter (VD AuthDomainException → 400) và GlobalExceptionMiddleware (→ 500) chạy SAU action filter
        // → đợi response gửi xong mới lấy được StatusCode thật rồi mới đẩy vào hàng đợi
        httpContext.Response.OnCompleted(() =>
        {
            objLog.StatusCode = httpContext.Response.StatusCode;
            objLog.IsError = objLog.StatusCode >= StatusCodes.Status400BadRequest || objException != null;
            if (objException != null && objLog.StatusCode >= StatusCodes.Status500InternalServerError)
            {
                string strStackTrace = objException.ToString();
                objLog.StackTrace = strStackTrace.Length > MaxStackTraceLength ? strStackTrace[..MaxStackTraceLength] : strStackTrace;
            }
            if (!objChannel.Writer.TryWrite(objLog)) Log.Warning("Hàng đợi ApiExecutionLog đầy — bỏ log {ApiName}", objLog.ApiName);
            return Task.CompletedTask;
        });
    }

    private static string SerializeArguments(IDictionary<string, object?> dicArguments)
    {
        Dictionary<string, object?> dicLogged = dicArguments.Where(x => x.Value is not CancellationToken).ToDictionary(x => x.Key, x => x.Value switch
        {
            IFormFile objFile => $"[file {objFile.FileName}, {objFile.Length} bytes]",
            IFormFileCollection lstFile => $"[{lstFile.Count} file]",
            Stream => "[stream]",
            _ => x.Value
        });
        return dicLogged.Count == 0 ? string.Empty : SerializeForLog(dicLogged);
    }

    private static string GetResponseBody(ActionExecutedContext objExecuted)
    {
        object? objValue = objExecuted.Result switch
        {
            ObjectResult objResult => objResult.Value,
            JsonResult objResult => objResult.Value,
            ContentResult objResult => objResult.Content,
            _ => null
        };
        return objValue switch
        {
            null => string.Empty,
            string strValue => strValue,
            _ => SerializeForLog(objValue)
        };
    }

    private static string SerializeForLog(object objValue)
    {
        try
        {
            return JsonSerializer.Serialize(objValue, ApiLogSanitizer.JsonOptions);
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "Không serialize được dữ liệu để ghi ApiExecutionLog");
            return "[không serialize được]";
        }
    }

    // Chỉ để tra log, không dùng cho bảo mật (header có thể bị giả)
    private static string GetClientIP(HttpContext httpContext)
    {
        string? strForwarded = httpContext.Request.Headers["CF-Connecting-IP"].FirstOrDefault() ?? httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim();
        return string.IsNullOrEmpty(strForwarded) ? httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty : strForwarded;
    }

    private static string FormatExecutionTime(long lngMs)
    {
        if (lngMs < 1000) return $"{lngMs} ms";
        double dblSeconds = lngMs / 1000.0;
        if (dblSeconds < 60) return $"{dblSeconds:F2} s";
        double dblMinutes = dblSeconds / 60;
        return dblMinutes < 60 ? $"{dblMinutes:F2} min" : $"{dblMinutes / 60:F2} h";
    }
}
