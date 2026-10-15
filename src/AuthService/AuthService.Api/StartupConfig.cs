using System.IdentityModel.Tokens.Jwt;
using AuthService.Application;
using AuthService.Application.Interfaces;
using AuthService.Application.Options;
using AuthService.Infrastructure;
using AuthService.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Api
{
    public static class StartupConfig
    {
        public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddInfrastructureServices(configuration);
            services.AddApplicationServices();

            AddJwtAuthentication(services);

            return services;
        }

        // JWT Bearer cho chính AuthService.Api (bảo vệ /auth/sessions, /auth/users...) — verify bằng PUBLIC key
        // của RsaSigningKeyRing (khóa hiện tại + khóa cũ khi xoay khóa). ClockSkew=30s khớp quyết định mục 32.
        private static void AddJwtAuthentication(IServiceCollection services)
        {
            // Claim "sub" giữ nguyên tên (không bị JwtSecurityTokenHandler đổi sang
            // ClaimTypes.NameIdentifier theo default map cũ) — khớp AuthApiControllerBase đọc
            // JwtRegisteredClaimNames.Sub trực tiếp.
            JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
            // Cấu hình sau khi DI dựng xong để lấy khóa/issuer/audience từ RsaSigningKeyRing + AuthOptions (không đọc config 2 nơi)
            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<RsaSigningKeyRing, IOptions<AuthOptions>>((options, keyRing, authOptions) =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = authOptions.Value.Issuer,
                        // Chỉ nhận token người dùng — token đối tác (M2M) khác audience bị từ chối ở mọi endpoint AuthService
                        ValidateAudience = true,
                        ValidAudience = authOptions.Value.UserAudience,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKeys = keyRing.ValidationKeys,
                        ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                        ClockSkew = TimeSpan.FromSeconds(30)
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            if (context.Principal?.FindFirst("token_use")?.Value != JwtService.TokenUseUser
                                || !Guid.TryParse(context.Principal.FindFirst("session_id")?.Value, out var guidSessionID))
                            {
                                context.Fail("Token không phải token người dùng.");
                                return;
                            }

                            // Token của phiên đã thu hồi (logout/đổi role/disable...) bị từ chối ngay, không chờ hết hạn 5 phút
                            var revocationCache = context.HttpContext.RequestServices.GetRequiredService<ISessionRevocationCache>();
                            if (await revocationCache.IsRevokedAsync(guidSessionID, context.HttpContext.RequestAborted))
                                context.Fail("Phiên đăng nhập đã bị thu hồi.");
                        }
                    };
                });

            services.AddAuthorization();
        }
    }
}
