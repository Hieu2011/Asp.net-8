using ApiCore8;
using ApiCore8.Midleware;
using ApiCore8.Services;
using BLL;
using Core;
using Core.Database;
using Core.Logging;
using Microsoft.Extensions.Configuration;
using ML;
using MongoDB.Driver;
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

            // ✅ MongoDB Setup
            var mongoConn = builder.Configuration.GetConnectionString("MongoDB");
            var mongoClient = new MongoClient(mongoConn);
            builder.Services.AddSingleton(mongoClient);

            // ✅ MongoDB Logger Service (singleton) - MUST be registered BEFORE MongoDataFactory
            builder.Services.AddSingleton<IMongoLoggerService, MongoLoggerService>();

            // ✅ MongoDataFactory (singleton) - Inject IMongoLoggerService
            builder.Services.AddSingleton<IMongoDataFactory>(sp =>
                new MongoDataFactory(
                    sp.GetRequiredService<MongoClient>(),
                    sp.GetRequiredService<IConfiguration>(),
                    sp.GetService<IMongoLoggerService>() // ✅ Add this - GetService (not GetRequiredService) vì có thể null
                ));

            // ✅ Redis
            builder.Services.AddSingleton<IRedisConnectionService, RedisConnectionService>();
            builder.Services.AddScoped<IBLL_RedisRepository, BLL_RedisRepository>();

            //// ✅ BLL Services
            builder.Services.AddBLLServices();
            builder.Services.AddScoped<IBLL_SystemLogRepository, BLL_SystemLogRepository>();

            //// ✅ Background services
            builder.Services.AddSingleton(Channel.CreateUnbounded<ApiExecutionLog>());
            builder.Services.AddHostedService<ApiLogBackgroundService>();

            app = builder.Build();
            app.UseMiddleware<GlobalExceptionMiddleware>();


            // ✅ CREATE INDEXES ON STARTUP
            // ✅ Tách việc tạo Index ra một Task riêng để tránh block startup nếu DB chậm
            //_ = Task.Run(async () =>
            //{
            //    try
            //    {
            //        using (var scope = app.Services.CreateScope())
            //        {
            //            var mongoFactory = scope.ServiceProvider.GetService<IMongoDataFactory>();
            //            if (mongoFactory != null)
            //            {
            //                var apiLogMongo = mongoFactory.Create("APILogs");

            //                var apiLogIndexes = new List<CreateIndexModel<ApiExecutionLog>>
            //                {
            //                    // ✅ Hợp nhất Text Index (Chỉ được có 1 Text Index duy nhất trên 1 collection)
            //                    new CreateIndexModel<ApiExecutionLog>(
            //                        Builders<ApiExecutionLog>.IndexKeys.Combine(
            //                            Builders<ApiExecutionLog>.IndexKeys.Text(x => x.RequestBody),
            //                            Builders<ApiExecutionLog>.IndexKeys.Text(x => x.ResponseBody),
            //                            Builders<ApiExecutionLog>.IndexKeys.Text(x => x.ApiName)
            //                        ),
            //                        new CreateIndexOptions { Name = "txt_req_resp_api", Background = true }
            //                    ),

            //                    new CreateIndexModel<ApiExecutionLog>(
            //                        Builders<ApiExecutionLog>.IndexKeys.Descending(x => x.CreatedAt),
            //                        new CreateIndexOptions { Name = "idx_createdAt", Background = true }
            //                    ),

            //                    new CreateIndexModel<ApiExecutionLog>(
            //                        Builders<ApiExecutionLog>.IndexKeys.Ascending(x => x.ApiName).Descending(x => x.CreatedAt),
            //                        new CreateIndexOptions { Name = "idx_apiname_created", Background = true }
            //                    ),

            //                    new CreateIndexModel<ApiExecutionLog>(
            //                        Builders<ApiExecutionLog>.IndexKeys.Ascending(x => x.Method),
            //                        new CreateIndexOptions { Name = "idx_method", Background = true }
            //                    ),

            //                    new CreateIndexModel<ApiExecutionLog>(
            //                        Builders<ApiExecutionLog>.IndexKeys.Descending(x => x.ExecutionMs),
            //                        new CreateIndexOptions { Name = "idx_executionms", Background = true }
            //                    ),

            //                    new CreateIndexModel<ApiExecutionLog>(
            //                        Builders<ApiExecutionLog>.IndexKeys.Ascending(x => x.ClientIP),
            //                        new CreateIndexOptions { Name = "idx_clientip", Background = true }
            //                    ),

            //                    // ✅ TTL Index: Xóa sau 90 ngày
            //                    new CreateIndexModel<ApiExecutionLog>(
            //                        Builders<ApiExecutionLog>.IndexKeys.Ascending(x => x.CreatedAt),
            //                        new CreateIndexOptions
            //                        {
            //                            Name = "idx_createdAt_ttl",
            //                            ExpireAfter = TimeSpan.FromDays(90),
            //                            Background = true
            //                        }
            //                    )
            //                };

            //                await apiLogMongo.CreateIndex(apiLogIndexes);
            //                Log.Information("✅ APILogs indexes ensured");
            //            }
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        Log.Error(ex, "❌ Failed to create MongoDB indexes: {Message}", ex.Message);
            //    }
            //});

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

