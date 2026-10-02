using AuthService.Application.Interfaces;
using AuthService.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Application Service / Use Case (mục 5b) — orchestration, Controller chỉ gọi thẳng đây.
            // Interface + implementation đều nằm ở Application vì không tự làm I/O.
            services.AddScoped<IAuthService, AuthenticationService>();
            services.AddScoped<IPasswordRecoveryService, PasswordRecoveryService>();
            services.AddScoped<IGoogleAuthService, GoogleAuthService>();
            services.AddScoped<IUserManagementService, UserManagementService>();
            services.AddScoped<ISessionManagementService, SessionManagementService>();
            services.AddScoped<IClientTokenService, ClientTokenService>();

            return services;
        }
    }
}
