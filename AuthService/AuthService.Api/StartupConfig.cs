using AuthService.Application;
using AuthService.Infrastructure;

namespace AuthService.Api
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
