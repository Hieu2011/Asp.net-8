using Shared.Logging;
using Shared.Common.Helpers;
using ApiCore8.Api;
using ApiCore8.Api.Middleware;
using ApiCore8.Api.Services;
using ApiCore8.Application.Services;
using ApiCore8.Domain.Entities;
using ApiCore8.Infrastructure;
using ApiCore8.Infrastructure.Mongo;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Serilog;
using Serilog.Events;
using System.Threading.Channels;

internal class Program
{
    private static async Task Main(string[] args)
    {
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

            //// ✅ Background services
            builder.Services.AddSingleton(Channel.CreateUnbounded<ApiExecutionLog>());
            builder.Services.AddHostedService<ApiLogBackgroundService>();

            app = builder.Build();
            app.UseMiddleware<GlobalExceptionMiddleware>();

            // ✅ Đảm bảo index/TTL cho SystemLogs tồn tại (idempotent, không chặn khởi động nếu lỗi)
            using (var scope = app.Services.CreateScope())
            {
                var mongoDb = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
                var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                var systemLogsCollection = config["Database:SystemLogsCollection"] ?? SystemLogRepository.DefaultCollectionName;
                await MongoIndexInitializer.EnsureSystemLogIndexesAsync(mongoDb, systemLogsCollection);
            }

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
                : httpContext.Request.Path.StartsWithSegments("/health") ? LogEventLevel.Verbose : LogEventLevel.Information);

            app.MapControllers();
            app.MapHealthChecks("/health");

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
