using AuthService.Api;
using AuthService.Api.Filters;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using Shared.Common.Health;
using Shared.Common.Middlewares;
using Shared.Logging;
using Shared.Logging.ApiLog;

internal class Program
{
    private static async Task Main(string[] args)
    {
        if (HealthCheckProbe.IsProbeMode(args))
        {
            Environment.ExitCode = await HealthCheckProbe.RunAsync();
            return;
        }
        WebApplication app = null!;
        try
        {
            var builder = WebApplication.CreateBuilder(args);

            // Console/File/Graylog/Mongo bật tắt theo Serilog:EnableLogging:*
            builder.AddSerilog();

            builder.Services.AddControllers(options => options.Filters.Add<AuthDomainExceptionFilter>());
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                var bearerScheme = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Nhập access token (không cần tiền tố \"Bearer \")",
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                };
                options.AddSecurityDefinition("Bearer", bearerScheme);
                options.AddSecurityRequirement(new OpenApiSecurityRequirement { { bearerScheme, Array.Empty<string>() } });
            });

            builder.Services.AddHealthChecks();
            builder.Services.AddAppServices(builder.Configuration);
            builder.Services.AddAuthRateLimiter();
            // Log thực thi API ([LogApi]) → Mongo, đã che password/OTP/token
            builder.Services.AddApiExecutionLog(builder.Configuration);

            app = builder.Build();
            // Index + TTL cho AuthSystemLogs/AuthApiLogs (idempotent, lỗi không chặn khởi động)
            await app.EnsureLogIndexesAsync();

            // Đầu pipeline: exception chưa bắt (ngoài AuthDomainException) → APIResult JSON thay vì trang lỗi text
            app.UseGlobalExceptionHandler();
            // Chỉ máy dev mới có Swagger — Staging/Production không lộ toàn bộ bề mặt API
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseHttpsRedirection();
            app.UseRouting();
            // Sau UseRouting để đọc được [EnableRateLimiting] của endpoint; trước Authentication để request bị chặn không tốn công verify JWT
            app.UseRateLimiter();

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
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}
