using BLL;

namespace ApiCore8
{
    public static class StartupConfig
    {
        public static void AddBLLServices(this IServiceCollection services)
        {
            services.AddScoped<IBLL_ApiLogRepository, BLL_ApiLogRepository>();
        }
    }
}
