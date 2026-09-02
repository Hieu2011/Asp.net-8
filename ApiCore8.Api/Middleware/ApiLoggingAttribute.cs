using ApiCore8.Application.Interfaces;
using ApiCore8.Domain.Entities;
using ApiCore8.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Serilog;
using System.Diagnostics;
using System.Text;
using System.Threading.Channels;

namespace ApiCore8.Api.Middleware
{
    public class LogApiAttribute : ActionFilterAttribute
    {
        // Giới hạn an toàn để không vượt MaxDocumentSize 16MB của MongoDB khi response/request quá
        // lớn (VD: list vài chục-trăm ngàn dòng) — cắt bớt, không log nguyên văn.
        private const int MaxLoggedBodyLength = 50_000;

        private DateTime? _startTime = null;
        private Stopwatch _stopwatch;
        private string _requestBody = string.Empty;
       
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Thời gian bắt đầu — lưu UTC, đồng bộ với SystemLog (chỉ convert +7 khi hiển thị)
            _startTime = DateTime.UtcNow;
            _stopwatch = Stopwatch.StartNew();  

            // Đọc ActionArguments (nếu có)
            if (context.ActionArguments != null && context.ActionArguments.Count > 0)
            {
                _requestBody = string.Join(", ", context.ActionArguments.Select(kvp => $"{kvp.Key}: {JsonConvert.SerializeObject(kvp.Value)}"));
            }
            else
            {
                // Nếu không có ActionArguments, đọc body từ HttpContext.Request.Body
                _requestBody = await ReadRequestBodyAsync(context.HttpContext);
            }

            // Thực thi Action
            var executedContext = await next();
                
            // Sau khi Action thực thi xong
            _stopwatch.Stop();
            var endTime = DateTime.UtcNow;
            var elapsedMilliseconds = _stopwatch.ElapsedMilliseconds;

            // Swagger UI (Swashbuckle) không tự hiển thị thời gian gọi trong panel kết quả — nhưng
            // có hiển thị response headers, nên gắn thời gian xử lý vào đây để thấy được trực tiếp
            // trong Swagger, không cần mở log/DevTools.
            context.HttpContext.Response.Headers["X-Response-Time-Ms"] = elapsedMilliseconds.ToString();

            var request = context.HttpContext.Request;
            var clientIp = context.HttpContext.Connection.RemoteIpAddress?.ToString();
            // Status code chính xác nhất: ưu tiên Result nếu nó expose StatusCode, ngược lại lấy từ HttpContext.Response
            int statusCode = context.HttpContext.Response.StatusCode;

            // Lấy response body từ executedContext.Result (nếu có)
            string responseBody = await ReadResponseBodyAsync(executedContext);
            // Tạo log object
            var repo = context.HttpContext.RequestServices.GetService<IApiLogRepository>();
            if (repo != null)
            {
                var log = new ApiExecutionLog
                {
                    ApiName = $"{request.Method} {request.Path}",
                    Method = request.Method,
                    RequestBody = Truncate(_requestBody),
                    ResponseBody = Truncate(responseBody),
                    StartTime = _startTime ?? DateTime.UtcNow,
                    EndTime = endTime,
                    CreatedAt = DateTime.UtcNow,
                    ClientIP = LogHelper.GetClientIp()
                };
                try
                {
                    var start = log.StartTime ?? log.EndTime ?? DateTime.UtcNow;
                    var end = log.EndTime ?? log.StartTime ?? DateTime.UtcNow;
                    log.ExecutionMs = (long)(end - start).TotalMilliseconds;
                }
                catch
                {
                    log.ExecutionMs = 0;
                }
                log.ExecutionTimeDisplay = FormatExecutionTime(log.ExecutionMs);

                // Non-blocking: đẩy task ghi log ra background, không thay đổi response nếu ghi log thất bại
                var channel = context.HttpContext.RequestServices
                    .GetRequiredService<Channel<ApiExecutionLog>>();
                if (channel != null)
                {
                    await channel.Writer.WriteAsync(log);
                }
                else
                {
                    Log.Warning("Channel<ApiExecutionLog> not registered - log will be lost");
                }
            }
        }
        // Tự quy đổi đơn vị theo độ lớn — tránh nhìn số mili giây trần trụi (VD 707) mà không rõ
        // là 707ms hay tưởng nhầm 707 giây/phút.
        private static string FormatExecutionTime(long ms)
        {
            if (ms < 1000) return $"{ms} ms";

            var seconds = ms / 1000.0;
            if (seconds < 60) return $"{seconds:F2} s";

            var minutes = seconds / 60;
            if (minutes < 60) return $"{minutes:F2} min";

            var hours = minutes / 60;
            return $"{hours:F2} h";
        }

        private static string Truncate(string body)
        {
            if (string.IsNullOrEmpty(body) || body.Length <= MaxLoggedBodyLength)
                return body;

            return body.Substring(0, MaxLoggedBodyLength) + $"...[truncated, original length {body.Length} chars]";
        }

        private static async Task<string> ReadResponseBodyAsync(ActionExecutedContext executedContext)
        {
            string responseBody = string.Empty;
            try
            {
                if (executedContext.Result != null)
                {
                    switch (executedContext.Result)
                    {
                        case ObjectResult objRes:
                            responseBody = objRes.Value != null ? JsonConvert.SerializeObject(objRes.Value) : string.Empty;
                            break;
                        case JsonResult jsonRes:
                            responseBody = jsonRes.Value != null ? JsonConvert.SerializeObject(jsonRes.Value) : string.Empty;
                            break;
                        case ContentResult contentRes:
                            responseBody = contentRes.Content ?? string.Empty;
                            break;
                        case StatusCodeResult codeRes:
                            responseBody = string.Empty;
                            break;
                        default:
                            responseBody = string.Empty;
                            break;
                    }
                }
                else if (executedContext.Exception != null)
                {
                    responseBody = executedContext.Exception.ToString();
                }
                return responseBody;
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to extract response body for logging: {Message}", ex.Message);
                return string.Empty;
            }
        }
        private async Task<string> ReadRequestBodyAsync(HttpContext context)
        {
            try
            {
                context.Request.EnableBuffering(); // Cho phép đọc lại body nhiều lần
                context.Request.Body.Position = 0; // Đặt vị trí về đầu stream

                using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true))
                {
                    var body = await reader.ReadToEndAsync();
                    context.Request.Body.Position = 0; // Reset lại vị trí stream sau khi đọc
                    return string.IsNullOrWhiteSpace(body) ? "None" : body;
                }
            }
            catch
            {
                return "Error reading request body.";
            }
        }
        
    }
}
