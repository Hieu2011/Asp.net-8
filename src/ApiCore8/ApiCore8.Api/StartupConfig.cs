using ApiCore8.Application;
using ApiCore8.Infrastructure;

namespace ApiCore8.Api
{
    public static class StartupConfig
    {
        public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddInfrastructureServices(configuration);
            services.AddApplicationServices();

            return services;
        }
    }
}
