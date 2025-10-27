using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public static class ConfigHelper
    {
        private static readonly IConfigurationRoot _config;

        static ConfigHelper()
        {
            _config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }

        /// <summary>
        /// Lấy connection string từ appsettings.json
        /// </summary>
        public static string GetConnectionString(string name = "Postgres")
            => _config.GetConnectionString(name);

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
        public static T GetValue<T>(string key, T defaultValue = default)
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
    }
}
