using ApiCore8.Application.Interfaces;
using ApiCore8.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ApiCore8.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IApiLogRepository, ApiLogRepository>();
            services.AddScoped<IRedisCacheRepository, RedisCacheRepository>();
            services.AddScoped<ISystemLogRepository, SystemLogRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            
            return services;
        }
    }
}
