using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog;
using Serilog.Sinks.Graylog;
using Serilog.Sinks.Graylog.Core.Transport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;

namespace Core
{
    public static class LogHelper 
    {
        public enum LogType
        {
            Info,
            Error,
            Debug,
            Warning
        }

        public static string GetClientIp()
        {
            try
            {
                string hostName = Dns.GetHostName();
                var ipEntry = Dns.GetHostEntry(hostName);
                var ipAddress = ipEntry.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                return ipAddress?.ToString() ?? "Unknown IP";
            }
            catch
            {
                return "127.0.0.1";
            }
        }
        private static string logTemplate = "{ApiName}. {QueryParameters}. {BodyParameters}. {StatusCode}.  {ClientIp}. {StartTime}. {EndTime}. {ExecutionTimeMs}";

        // **Thêm hàm ghi log tổng hợp**
        public static string AddLog(string apiName, string queryParams, string bodyParams, int statusCode, string userLogin = "administrator", DateTime? startTime = null, DateTime? endTime = null, LogType enErrorTypes = LogType.Info)
        {
            try
            {
                string strResult = "";
                startTime ??= DateTime.UtcNow;
                endTime ??= DateTime.UtcNow;
                var executionTime = (endTime - startTime).Value.TotalMilliseconds;
                object[] args = new object[8] { apiName, queryParams, bodyParams,
                statusCode, GetClientIp(), startTime.Value.ToString("yyyy-MM-dd HH:mm:ss.fff") , endTime.Value.ToString("yyyy-MM-dd HH:mm:ss.fff") , executionTime};

                var Column = new
                {
                    ApiName = apiName,
                    QueryParameters = queryParams,
                    BodyParameters = bodyParams,
                    StatusCode = statusCode,
                    ClientIp = GetClientIp(),
                    StartTime = startTime.Value.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    EndTime = endTime.Value.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    ExecutionTimeMs = executionTime
                };
                switch (enErrorTypes)
                {
                    case LogType.Error:
                        Log.Logger.Error(logTemplate, args);
                        break;
                    case LogType.Info:
                        Log.Logger.Information(logTemplate, args);
                        break;
                    case LogType.Warning:
                        Log.Logger.Warning(logTemplate, args);
                        break;
                    case LogType.Debug:
                        Log.Logger.Debug(logTemplate, args);
                        break;
                    default:
                        Log.Logger.Information(logTemplate, args);
                        break;

                }
                return strResult;
            }
            catch (Exception ex)
            {
              return ex.CreateExceptionMessage();
            }
        }

        public static string AddLog(this ILogger<object> logger, string apiName = "", string queryParams = "", string bodyParams = "", int statusCode = 0, string userLogin = "administrator", DateTime? startTime = null, DateTime? endTime = null, LogType enErrorTypes = LogType.Info)
        {
            try
            {
                string strResult = "";

                startTime ??= DateTime.Now;
                endTime ??= DateTime.Now;
                var executionTime = (endTime - startTime).Value.TotalMilliseconds;
                object[] args = new object[8] { apiName, queryParams, bodyParams,
                statusCode, GetClientIp(), startTime.Value.ToString("yyyy-MM-dd HH:mm:ss.fff") , endTime.Value.ToString("yyyy-MM-dd HH:mm:ss.fff") , executionTime};

                var Column = new
                {
                    ApiName = apiName,
                    QueryParameters = queryParams,
                    BodyParameters = bodyParams,
                    StatusCode = statusCode,
                    ClientIp = GetClientIp(),
                    StartTime = startTime.Value.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    EndTime = endTime.Value.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    ExecutionTimeMs = executionTime
                };
                switch (enErrorTypes)
                {
                    case LogType.Error:
                        logger.LogError(logTemplate, args);
                        break;
                    case LogType.Info:
                        logger.LogInformation(logTemplate, args);
                        break;
                    case LogType.Warning:
                        logger.LogWarning(logTemplate, args);
                        break;
                    case LogType.Debug:
                        logger.LogDebug(logTemplate, args);
                        break;
                    default:
                        logger.LogInformation(logTemplate, args);
                        break;

                }
                return strResult;
            }
            catch (Exception ex)
            {
                return ex.CreateExceptionMessage();
            }
        }

    }

}
