using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
using static Core.LogHelper;

namespace Core
{
    public static class LoggingStartupConfig
    {
        public static void AddSerilog(this WebApplicationBuilder builder)
        {
            // Đọc cấu hình từ appsettings.json
            var loggerConfig = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext();

            // Khởi tạo Logger
            Log.Logger = loggerConfig.CreateLogger();

            // Tích hợp Serilog vào hệ thống log của ứng dụng
            builder.Host.UseSerilog();
        }
        public static void SerilogConfig(this ConfigureHostBuilder hostBuilder)
        {
            // Load configuration từ appsettings.json
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Kiểm tra bật/tắt logging
            var enableFileLogging = configuration.GetValue<bool>("Logging:EnableFileLogging");
            var enableGraylog = configuration.GetValue<bool>("Logging:EnableGraylog");

            // Cấu hình Serilog
            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Information()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                ;

            if (enableFileLogging)
            {
                loggerConfig.WriteTo.File(
                    path: configuration["Logging:FileLogging:Path"],
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: configuration.GetValue<int>("Logging:FileLogging:RetainedFileCountLimit")
                );
            }

            if (enableGraylog)
            {
                loggerConfig.WriteTo.Graylog(new GraylogSinkOptions
                {
                    HostnameOrAddress = configuration["Logging:Graylog:HostnameOrAddress"],
                    Port = configuration.GetValue<int>("Logging:Graylog:Port"),
                    Facility = configuration["Logging:Graylog:Facility"],
                    TransportType = TransportType.Udp
                });
            }

            Log.Logger = loggerConfig.CreateLogger();
            hostBuilder.UseSerilog();

          
        }
       
    }
}
