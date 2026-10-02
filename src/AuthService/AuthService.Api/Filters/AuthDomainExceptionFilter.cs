using Shared.Common.Contracts;
using AuthService.Application;
using AuthService.Application.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AuthService.Api.Filters
{
    /// <summary>
    /// Bắt <see cref="AuthDomainException"/> tập trung 1 chỗ — map ErrorCode → APIResult.StatusID,
    /// để Controller (mục 8 Bước 3) KHÔNG cần try/catch lặp lại ở từng action (giữ Controller mỏng,
    /// chỉ gọi Service rồi wrap kết quả thành công).
    /// </summary>
    public class AuthDomainExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception is not AuthDomainException ex)
                return;

            if (ex.RetryAfter.HasValue)
            {
                context.HttpContext.Response.Headers.RetryAfter = ((int)ex.RetryAfter.Value.TotalSeconds).ToString();
            }

            context.Result = new ObjectResult(APIResult.Error(ex.ErrorCode, ex.Message))
            {
                StatusCode = MapStatusCode(ex.ErrorCode)
            };
            context.ExceptionHandled = true;
        }

        private static int MapStatusCode(AuthErrorCode errorCode) => errorCode switch
        {
            AuthErrorCode.Forbidden => StatusCodes.Status403Forbidden,
            AuthErrorCode.SelfTargetNotAllowed => StatusCodes.Status403Forbidden,
            AuthErrorCode.SessionNotFound => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest
        };
    }
}
