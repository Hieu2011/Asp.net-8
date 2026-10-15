using Shared.Logging;
using Shared.Logging.ApiLog;
using Shared.Common.Health;
using Shared.Common.Helpers;
using ApiCore8.Api;
using ApiCore8.Api.Middleware;
using Serilog;
using Serilog.Events;

internal class Program
{
    private static async Task Main(string[] args)
    {
        if (HealthCheckProbe.IsProbeMode(args))
        {
            Environment.ExitCode = await HealthCheckProbe.RunAsync();
            return;
        }
        WebApplication app = null;
        string templateLog = "{Title}. {Content}.";
        try
        {
            var builder = WebApplication.CreateBuilder(args);

            // ✅ Configure Serilog
            builder.AddSerilog();

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddHealthChecks();

            // ✅ Infrastructure (Mongo/Redis/Postgres) + Application (repositories)
            builder.Services.AddAppServices(builder.Configuration);

            // Log thực thi API ([LogApi]) → Mongo, ghi nền qua hàng đợi
            builder.Services.AddApiExecutionLog(builder.Configuration);

            app = builder.Build();
            app.UseMiddleware<GlobalExceptionMiddleware>();

            // Index + TTL cho SystemLogs/APILogs (idempotent, lỗi không chặn khởi động)
            await app.EnsureLogIndexesAsync();

            // ✅ Log environment
            object[] arrayLog = new object[] { "Môi trường chạy HPM Service", $"Environment: {app.Environment.EnvironmentName}" };
            Log.Information(templateLog, arrayLog);

            // ✅ Configure middleware pipeline
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseHttpsRedirection();
            app.UseRouting();

            // ✅ Anti-DDoS Middleware (sẽ implement sau)
            //app.UseMiddleware<AntiSpamMiddleware>();

            app.UseAuthentication();
            app.UseAuthorization();
            // Healthcheck Coolify gọi liên tục → hạ xuống Verbose để không ghi log rác
            app.UseSerilogRequestLogging(options => options.GetLevel = (httpContext, _, exception) =>
                exception != null || httpContext.Response.StatusCode >= 500 ? LogEventLevel.Error
                // 4xx (401 token sai, 404, 429...) lên Warning để vẫn vào Mongo SystemLogs, kể cả request bị chặn trước controller
                : httpContext.Response.StatusCode >= 400 ? LogEventLevel.Warning
                : HealthCheckProbe.IsHealthRequest(httpContext) ? LogEventLevel.Verbose : LogEventLevel.Information);

            app.MapControllers();
            app.MapHealthProbe();

            await app.RunAsync();
        }
        catch (Exception exception)
        {
            object[] arrayLog = new object[] { "Lỗi khi khởi chạy HPM Service", $"Exception: {exception.CreateExceptionMessage()}" };
            Log.Error(templateLog, arrayLog);
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}
