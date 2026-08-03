using ApiCore8.Application.Interfaces;
using ApiCore8.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ApiCore8.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IBLL_ApiLogRepository, BLL_ApiLogRepository>();
            services.AddScoped<IBLL_RedisRepository, BLL_RedisRepository>();
            services.AddScoped<IBLL_SystemLogRepository, BLL_SystemLogRepository>();

            return services;
        }
    }
}
