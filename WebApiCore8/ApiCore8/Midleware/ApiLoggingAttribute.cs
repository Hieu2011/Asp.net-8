using Core;
using Microsoft.AspNetCore.Mvc.Filters;

using System.Diagnostics;
using System.Text;
using static Core.LogHelper;

namespace ApiCore8.Midleware
{
    public class LogApiAttribute : ActionFilterAttribute
    {
        private DateTime? _startTime = null;
        private Stopwatch _stopwatch;
        private string _requestBody = string.Empty;

       
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Thời gian bắt đầu
            _startTime = DateTime.Now;
            _stopwatch = Stopwatch.StartNew();

            // Đọc ActionArguments (nếu có)
            if (context.ActionArguments != null && context.ActionArguments.Count > 0)
            {
                _requestBody = string.Join(", ", context.ActionArguments.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
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
            var endTime = DateTime.Now; // Thời gian kết thúc
            var elapsedMilliseconds = _stopwatch.ElapsedMilliseconds; // Thời gian thực thi

            var request = context.HttpContext.Request;
            var clientIp = context.HttpContext.Connection.RemoteIpAddress?.ToString();
            var statusCode = context.HttpContext.Response.StatusCode;

            // Log qua LogHelper
            LogHelper.AddLog(
                apiName: $"{request.Method} {request.Path}",
                queryParams: request.QueryString.ToString(),
                bodyParams: _requestBody,
                statusCode: statusCode,
                startTime: _startTime,
                endTime: endTime
            );
            //_logger.AddLog(
            //    apiName: $"{request.Method} {request.Path} hip",
            //    queryParams: request.QueryString.ToString(),
            //    bodyParams: _requestBody,
            //    statusCode: statusCode,
            //    startTime: _startTime,
            //    endTime: endTime
            //);
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
            catch (Exception ex)
            {
                //_logHelper.LogError(ex, "Error reading request body.");
                return "Error reading request body.";
            }
        }
    }
}
