using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Registration/login/session services land here in later roadmap phases.
            return services;
        }
    }
}
