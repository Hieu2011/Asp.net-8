using ApiCore8.Api;
using ApiCore8.Api.Middleware;
using ApiCore8.Api.Services;
using ApiCore8.Domain.Entities;
using ApiCore8.Infrastructure;
using Serilog;
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

            // ✅ Infrastructure (Mongo/Redis/Postgres) + Application (BLL repositories)
            builder.Services.AddAppServices(builder.Configuration);

            //// ✅ Background services
            builder.Services.AddSingleton(Channel.CreateUnbounded<ApiExecutionLog>());
            builder.Services.AddHostedService<ApiLogBackgroundService>();

            app = builder.Build();
            app.UseMiddleware<GlobalExceptionMiddleware>();

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
            app.UseSerilogRequestLogging();

            app.MapControllers();

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
