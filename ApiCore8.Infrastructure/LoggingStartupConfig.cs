using ApiCore8.Application.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Serilog;
using Serilog.Sinks.Graylog;
using Serilog.Sinks.Graylog.Core.Transport;
using Serilog.Sinks.MongoDB;

namespace ApiCore8.Infrastructure
{
    public static class LoggingStartupConfig
    {
        public static void AddSerilog(this WebApplicationBuilder builder)
        {
            // CHẨN ĐOÁN TẠM: Serilog mặc định nuốt hết exception nội bộ của sink (vd Mongo auth/connect fail)
            // để không làm crash app — SelfLog là cách chính thức để xem log lỗi đó ra Console.
            Serilog.Debugging.SelfLog.Enable(msg => Console.Error.WriteLine($"[Serilog SelfLog] {msg}"));

            var configuration = builder.Configuration;

            var loggerConfig = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration) // Đọc cấu hình cơ bản
                .Enrich.FromLogContext();

            // Thêm logic kiểm tra cờ bật/tắt
            if (configuration.GetValue<bool>("Serilog:EnableLogging:Console"))
            {
                loggerConfig.WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}");
            }

            if (configuration.GetValue<bool>("Serilog:EnableLogging:File"))
            {
                loggerConfig.WriteTo.File("logs/log-.txt", rollingInterval: Serilog.RollingInterval.Day, retainedFileCountLimit: 7,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}");
            }

            if (configuration.GetValue<bool>("Serilog:EnableLogging:Graylog"))
            {
                loggerConfig.WriteTo.Graylog(new GraylogSinkOptions
                {
                    HostnameOrAddress = configuration["Serilog:Graylog:Hostname"] ?? "127.0.0.1",
                    Port = configuration.GetValue<int>("Serilog:Graylog:Port", 12201),
                    TransportType = TransportType.Udp,
                    Facility = configuration["Serilog:Graylog:Facility"] ?? "HPM"
                });
            }

            if (configuration.GetValue<bool>("Serilog:EnableLogging:Mongo"))
            {
                var mongoConnectionString = configuration.GetConnectionString("MongoDB")
                    ?? throw new InvalidOperationException("ConnectionStrings:MongoDB not configured");
                var mongoDatabase = configuration["Database:MongoDatabase"]
                    ?? throw new InvalidOperationException("Database:MongoDatabase not configured");
                var systemLogsCollection = configuration["Database:SystemLogsCollection"] ?? SystemLogRepository.DefaultCollectionName;

                // Dùng MongoUrlBuilder để gắn database name đúng cách, thay vì nối chuỗi thủ công —
                // nối chuỗi trực tiếp sẽ vỡ nếu connection string đã có query string (?directConnection=true...).
                var mongoUrlBuilder = new MongoUrlBuilder(mongoConnectionString);

                // QUAN TRỌNG: chốt AuthenticationSource TRƯỚC khi đổi DatabaseName.
                // Theo chuẩn Mongo URI, nếu không set authSource rõ ràng, driver tự suy ra authSource
                // = database hiện có trong URI (hoặc "admin" nếu URI chưa có database nào).
                // Đổi DatabaseName mà không chốt AuthenticationSource trước sẽ vô tình đổi luôn authSource
                // ngầm định theo → user xác thực sai database → MongoAuthenticationException.
                if (string.IsNullOrEmpty(mongoUrlBuilder.AuthenticationSource))
                {
                    mongoUrlBuilder.AuthenticationSource = string.IsNullOrEmpty(mongoUrlBuilder.DatabaseName)
                        ? "admin"
                        : mongoUrlBuilder.DatabaseName;
                }

                mongoUrlBuilder.DatabaseName = mongoDatabase;
                var mongoUrl = mongoUrlBuilder.ToMongoUrl();

                loggerConfig.WriteTo.MongoDBBson(
                    mongoUrl.ToString(),
                    collectionName: systemLogsCollection);
            }

            Log.Logger = loggerConfig.CreateLogger();

            builder.Host.UseSerilog();
        }
    }
}
