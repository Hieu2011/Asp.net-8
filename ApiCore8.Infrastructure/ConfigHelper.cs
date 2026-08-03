using Microsoft.Extensions.Configuration;

namespace ApiCore8.Infrastructure
{
    public static class ConfigHelper
    {
        private static readonly IConfigurationRoot _config;

        static ConfigHelper()
        {
            // ✅ Lấy environment từ biến môi trường
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            
            _config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true) // ✅ Đọc theo env
                .AddEnvironmentVariables() // ✅ Đọc env variables
                .Build();
        }

        /// <summary>
        /// Lấy connection string từ appsettings.json
        /// </summary>
        public static string GetConnectionString(string name = "Postgres")
            => _config.GetConnectionString(name) ?? string.Empty;

        /// <summary>
        /// Lấy giá trị theo key từ appsettings.json
        /// </summary>
        public static string GetValue(string key)
            => _config[key] ?? string.Empty;

        /// <summary>
        /// Lấy giá trị kiểu int
        /// </summary>
        public static int GetInt(string key, int defaultValue = 0)
            => int.TryParse(_config[key], out var result) ? result : defaultValue;

        /// <summary>
        /// Lấy giá trị kiểu bool
        /// </summary>
        public static bool GetBool(string key, bool defaultValue = false)
            => bool.TryParse(_config[key], out var result) ? result : defaultValue;

        /// <summary>
        /// Lấy giá trị theo kiểu dữ liệu generic T
        /// </summary>
        public static T? GetValue<T>(string key, T? defaultValue = default)
        {
            try
            {
                return _config.GetSection(key).Get<T>() ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }
        
        /// <summary>
        /// Lấy toàn bộ IConfiguration (cho trường hợp cần bind object)
        /// </summary>
        public static IConfiguration Configuration => _config;
    }
}
