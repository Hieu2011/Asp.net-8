using ApiCore8.Application.Interfaces;
using ApiCore8.Infrastructure.Mongo;
using ApiCore8.Infrastructure.Repositories;
using Shared.Caching.Redis;
using Shared.DataAccess.Abstractions;
using Shared.DataAccess.Database;
using ApiCore8.Infrastructure.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace ApiCore8.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            var mongoConnectionString = configuration.GetConnectionString("MongoDB");
            services.AddSingleton(new MongoClient(mongoConnectionString));

            services.AddSingleton<IMongoDatabase>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var dbName = config["Database:MongoDatabase"]
                    ?? throw new InvalidOperationException("Database:MongoDatabase not configured");
                return sp.GetRequiredService<MongoClient>().GetDatabase(dbName);
            });

            services.AddSingleton<IRedisConnectionService, RedisConnectionService>();

            // 1 key connection string duy nhất — đổi DB chỉ cần đổi giá trị "ConnectionStrings:Database"
            // trong config (không đổi code). Provider (Postgres/Oracle/SqlServer) tự nhận diện qua
            // ConnectionStringDetector, không cần khai báo riêng. Giữ tạm fallback đọc "Postgres"
            // (key cũ) cho tới khi User Secrets được đổi tên sang "Database".
            // DbHelper giữ 1 connection riêng cho vòng đời của nó → đăng ký Scoped (1 instance/request),
            // không phải Singleton như Mongo/Redis (những cái đó tự quản lý pool nội bộ).
            services.AddScoped<IDataCore>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var connectionString = config.GetConnectionString("Database")
                    ?? config.GetConnectionString("Postgres")
                    ?? throw new InvalidOperationException("ConnectionStrings:Database not configured");

                return DataCoreFactory.Create(connectionString);
            });

            services.AddScoped<IApiLogRepository, ApiLogRepository>();
            services.AddScoped<ISystemLogRepository, SystemLogRepository>();
            services.AddScoped<IRedisCacheRepository, RedisCacheRepository>();
            services.AddScoped<IUserRepository, UserRepository>();

            return services;
        }
    }
}
