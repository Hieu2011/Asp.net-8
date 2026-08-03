using ApiCore8.Application.Abstractions;
using ApiCore8.Infrastructure.Logging;
using ApiCore8.Infrastructure.Mongo;
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

            // MUST be registered before IMongoDataFactory
            services.AddSingleton<IMongoLoggerService, MongoLoggerService>();

            services.AddSingleton<IMongoDataFactory>(sp =>
                new MongoDataFactory(
                    sp.GetRequiredService<MongoClient>(),
                    sp.GetRequiredService<IConfiguration>(),
                    sp.GetService<IMongoLoggerService>()
                ));

            services.AddSingleton<IRedisConnectionService, RedisConnectionService>();

            return services;
        }
    }
}
