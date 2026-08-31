using ApiCore8.Infrastructure;
using System.Net;

namespace ApiCore8.Api.Middleware
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
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            string requestID = StringUtilities.GenarateRandomString(10);
            _logger.LogError(exception, "Lỗi gọi API HPM-Internal ({RequestId})", requestID);
            var responseMessage = new { isError = true, status = 200, message = $"Lỗi gọi API HPM-Internal ({requestID})", messageDetail = $"Lỗi {exception.CreateExceptionMessage(requestID)}, vui lòng liên hệ IT , team HPM-Internal" };
            return context.Response.WriteAsJsonAsync(responseMessage);
        }
    }
}
