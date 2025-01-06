using Core;
using System.Net;

namespace ApiCore8.Midleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _logger = logger;
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                await HandleExceptionAsync(context, exception);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            string requestID = StringUtilities.GenarateRandomString(10);
            _logger.AddLog($"Lỗi gọi API HPM-Internal ({requestID})", exception.CreateExceptionMessage(requestID));
            var responseMessage = new { isError = true, status = 200, message = $"Lỗi gọi API HPM-Internal ({requestID})", messageDetail = $"Lỗi {exception.Message}, vui lòng liên hệ IT Tận Tâm, team HPM-Internal" };
            return context.Response.WriteAsJsonAsync(responseMessage);
        }
    }
}
