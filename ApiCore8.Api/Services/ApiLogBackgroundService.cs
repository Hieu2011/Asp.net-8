using ApiCore8.Application.Interfaces;
using ApiCore8.Domain.Entities;
using Serilog;
using System.Threading.Channels;

namespace ApiCore8.Api.Services
{
    public class ApiLogBackgroundService : BackgroundService
    {
        private readonly Channel<ApiExecutionLog> _channel;
        private readonly IServiceScopeFactory _scopeFactory;

        public ApiLogBackgroundService(
            Channel<ApiExecutionLog> channel,
            IServiceScopeFactory scopeFactory)
        {
            _channel = channel;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var log in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IBLL_ApiLogRepository>();

                try
                {
                    var res = await repo.InsertLog(log);
                   
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Exception when inserting ApiExecutionLog");
                }
            }
        }
    }
}
