using System.Threading.Channels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Shared.Logging.ApiLog;

public static class ApiLogStartupConfig
{
    public const string DefaultCollection = "APILogs";
    private const int QueueCapacity = 10_000;

    /// <summary>Đăng ký ghi log [LogApi] vào Mongo — đọc ConnectionStrings:MongoDB, Database:MongoDatabase, Database:MongoCollection.</summary>
    public static IServiceCollection AddApiExecutionLog(this IServiceCollection services, IConfiguration configuration)
    {
        string strConnectionString = configuration.GetConnectionString("MongoDB") is { Length: > 0 } strValue ? strValue : throw new InvalidOperationException("ConnectionStrings:MongoDB chưa cấu hình.");
        string strDatabase = configuration["Database:MongoDatabase"] ?? throw new InvalidOperationException("Database:MongoDatabase chưa cấu hình.");
        string strCollection = configuration["Database:MongoCollection"] ?? DefaultCollection;
        services.AddSingleton(_ => new MongoClient(strConnectionString).GetDatabase(strDatabase).GetCollection<ApiExecutionLog>(strCollection));
        // Hàng đợi có giới hạn: Mongo chậm/chết thì bỏ log mới thay vì phình RAM
        services.AddSingleton(Channel.CreateBounded<ApiExecutionLog>(new BoundedChannelOptions(QueueCapacity) { SingleReader = true }));
        services.AddHostedService<ApiLogWriterService>();
        return services;
    }
}
