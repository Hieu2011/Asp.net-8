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
        private DateTime? _startTime = null;
        private Stopwatch _stopwatch;
        private string _requestBody = string.Empty;
       
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Thời gian bắt đầu (dùng UTC khuyến nghị)
            _startTime = DateTime.Now;
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
            var endTime = DateTime.Now;
            var elapsedMilliseconds = _stopwatch.ElapsedMilliseconds;

            var request = context.HttpContext.Request;
            var clientIp = context.HttpContext.Connection.RemoteIpAddress?.ToString();
            // Status code chính xác nhất: ưu tiên Result nếu nó expose StatusCode, ngược lại lấy từ HttpContext.Response
            int statusCode = context.HttpContext.Response.StatusCode;

            // Lấy response body từ executedContext.Result (nếu có)
            string responseBody = await ReadResponseBodyAsync(executedContext);
            // Tạo log object
            var repo = context.HttpContext.RequestServices.GetService<IBLL_ApiLogRepository>();
            if (repo != null)
            {
                var log = new ApiExecutionLog
                {
                    ApiName = $"{request.Method} {request.Path}",
                    Method = request.Method,
                    RequestBody = _requestBody,
                    ResponseBody = responseBody,
                    StartTime = _startTime ?? DateTime.Now,
                    EndTime = endTime,
                    CreatedAt = DateTime.Now,
                    ClientIP = LogHelper.GetClientIp(),
                    StartTimeStr = new System.DateTimeOffset(_startTime ?? DateTime.Now).ToString("yyyy-MM-dd HH:mm:ss.fff zzz"),
                    EndTimeStr = new System.DateTimeOffset(endTime).ToString("yyyy-MM-dd HH:mm:ss.fff zzz")
                };
                try
                {
                    var start = log.StartTime ?? log.EndTime ?? DateTime.Now;
                    var end = log.EndTime ?? log.StartTime ?? DateTime.Now;
                    log.ExecutionMs = (long)(end - start).TotalMilliseconds;
                }
                catch
                {
                    log.ExecutionMs = 0;
                }

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
