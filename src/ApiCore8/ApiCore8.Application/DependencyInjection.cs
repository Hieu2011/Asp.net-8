using ApiCore8.Application.Interfaces;
using ApiCore8.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ApiCore8.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {

            // Application Service / Use Case — orchestration, Controller chỉ gọi thẳng đây
            services.AddScoped<IApiLogUseCase, ApiLogUseCase>();
            services.AddScoped<ISystemLogUseCase, SystemLogUseCase>();

            return services;
        }
    }
}
