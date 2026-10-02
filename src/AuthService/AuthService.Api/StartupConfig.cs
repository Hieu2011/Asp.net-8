using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using AuthService.Application;
using AuthService.Application.Options;
using AuthService.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Api
{
    public static class StartupConfig
    {
        public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddInfrastructureServices(configuration);
            services.AddApplicationServices();

            AddJwtAuthentication(services, configuration);

            return services;
        }

        // JWT Bearer cho chính AuthService.Api (bảo vệ /auth/sessions, /auth/users...) — cùng cặp
        // RSA key JwtService dùng để ký (mục 4/16/43). ClockSkew=30s khớp quyết định mục 32.
        private static void AddJwtAuthentication(IServiceCollection services, IConfiguration configuration)
        {
            var rsaPrivateKeyPem = configuration[$"{AuthOptions.SectionName}:{nameof(AuthOptions.RsaPrivateKey)}"]
                ?? throw new InvalidOperationException($"{AuthOptions.SectionName}:{nameof(AuthOptions.RsaPrivateKey)} chưa cấu hình.");

            var rsa = RSA.Create();
            rsa.ImportFromPem(rsaPrivateKeyPem);

            // Claim "sub" giữ nguyên tên (không bị JwtSecurityTokenHandler đổi sang
            // ClaimTypes.NameIdentifier theo default map cũ) — khớp AuthApiControllerBase đọc
            // JwtRegisteredClaimNames.Sub trực tiếp.
            JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new RsaSecurityKey(rsa),
                        ClockSkew = TimeSpan.FromSeconds(30)
                    };
                });

            services.AddAuthorization();
        }
    }
}
