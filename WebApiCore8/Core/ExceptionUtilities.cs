using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public static class ExceptionUtilities
    {
        public static string CreateExceptionMessage(this Exception exception, string requestId = "")
        {
            // Tạo Request_Id nếu không được truyền vào
            if (string.IsNullOrEmpty(requestId))
            {
                requestId = $"REQ-{Guid.NewGuid()}"; // Ví dụ mã REQ-xxxx
            }

            var stringBuilder = new StringBuilder();

            // Ghi Request_Id
            stringBuilder.AppendLine($"Request_Id: {requestId}");

            // Lặp qua Exception và InnerException
            Exception currentException = exception;
            while (currentException != null)
            {
                stringBuilder.AppendLine($"-> {currentException.Message}");
                if (!string.IsNullOrEmpty(currentException.StackTrace))
                {
                    stringBuilder.AppendLine(currentException.StackTrace);
                }
                currentException = currentException.InnerException;
            }

            return stringBuilder.ToString();
        }
    }
}
