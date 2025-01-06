using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public static class SerilogExtensions
    {
        public static IHostBuilder SerilogConfig(this IHostBuilder hostBuilder)
        {
            hostBuilder.UseSerilog((context, configuration) =>
            {
                configuration
                    .ReadFrom.Configuration(context.Configuration) // Đọc từ appsettings.json
                    .Enrich.FromLogContext()
                    .WriteTo.Console(); // Ghi log ra console
            });

            return hostBuilder;
        }
    }
}
