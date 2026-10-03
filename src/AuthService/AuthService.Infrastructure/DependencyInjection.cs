using Shared.DataAccess.Abstractions;
using AuthService.Application.Interfaces;
using AuthService.Application.Options;
using AuthService.Infrastructure.External;
using AuthService.Infrastructure.Postgres;
using AuthService.Infrastructure.Redis;
using Shared.DataAccess.Postgres;
using AuthService.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace AuthService.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Options pattern (mục 49) — bind + validate ngay lúc start-up, không chờ tới lúc có
            // request đầu tiên mới phát hiện thiếu config.
            services.AddOptions<AuthOptions>()
                .Bind(configuration.GetSection(AuthOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddOptions<SmtpOptions>()
                .Bind(configuration.GetSection(SmtpOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddOptions<TurnstileOptions>()
                .Bind(configuration.GetSection(TurnstileOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddOptions<GoogleOptions>()
                .Bind(configuration.GetSection(GoogleOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // Postgres — mỗi request 1 IDataCore mới (AddScoped), riêng auth_db (mục 2 roadmap-auth-sso).
            services.AddScoped<IDataCore>(sp =>
            {
                var connectionString = configuration.GetConnectionString("Postgres")
                    ?? throw new InvalidOperationException("ConnectionStrings:Postgres chưa cấu hình.");
                return new PostgresDbHelper(connectionString);
            });

            // Redis — 1 IConnectionMultiplexer dùng chung toàn app, tự quản lý pool nội bộ (Singleton).
            // RedisConnectionFactory (Shared.DataAccess) dùng chung với ApiCore8, tránh mỗi service tự
            // gọi ConnectionMultiplexer.Connect(...) riêng.
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var connectionString = configuration.GetConnectionString("Redis")
                    ?? throw new InvalidOperationException("ConnectionStrings:Redis chưa cấu hình.");
                return Shared.Caching.Redis.RedisConnectionFactory.Connect(connectionString);
            });

            // Repositories (sp_* qua PostgresDbHelper)
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ISessionRepository, SessionRepository>();
            services.AddScoped<IClientRepository, ClientRepository>();
            services.AddScoped<ILoginHistoryRepository, LoginHistoryRepository>();

            // Security adapters — stateless, Singleton (JwtService/TotpSecretEncryption tốn chi phí
            // khởi tạo 1 lần: import RSA key / derive AES key từ config).
            services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
            services.AddSingleton<ITotpSecretEncryption, TotpSecretEncryption>();
            services.AddSingleton<IOtpService, TotpService>();
            services.AddSingleton<IJwtService, JwtService>();
            services.AddSingleton<IRoleAuthorizationService, RoleAuthorizationService>();
            services.AddSingleton<IClientIpResolver, ClientIpResolver>();

            // Redis-backed services — Singleton, dùng chung IConnectionMultiplexer.
            services.AddSingleton<IOtpLockoutService, OtpFailCounterService>();
            services.AddSingleton<IPasswordResetService, PasswordResetService>();
            services.AddSingleton<IGoogleSetupTokenService, GoogleSetupTokenService>();
            services.AddSingleton<ISessionRevocationCache, SessionRevocationCache>();
            services.AddSingleton<IClientRateLimiter, ClientRateLimiter>();
            services.AddSingleton<IUserProfileCache, UserProfileCache>();

            // External adapters
            services.AddSingleton<IEmailSender, EmailSender>();
            services.AddSingleton<IGoogleTokenVerifier, GoogleTokenVerifier>();
            services.AddHttpClient<ICaptchaVerifier, CaptchaVerifier>();

            return services;
        }
    }
}
