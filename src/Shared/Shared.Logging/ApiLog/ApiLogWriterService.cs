using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using Serilog;

namespace Shared.Logging.ApiLog;

/// <summary>Đọc hàng đợi ApiExecutionLog và ghi vào Mongo ở nền.</summary>
public sealed class ApiLogWriterService : BackgroundService
{
    private readonly Channel<ApiExecutionLog> _channel;
    private readonly IMongoCollection<ApiExecutionLog> _collection;

    public ApiLogWriterService(Channel<ApiExecutionLog> channel, IMongoCollection<ApiExecutionLog> collection)
    {
        _channel = channel;
        _collection = collection;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (ApiExecutionLog objLog in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await _collection.InsertOneAsync(objLog, cancellationToken: stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                Log.Error(exception, "Lỗi ghi ApiExecutionLog {ApiName} vào Mongo", objLog.ApiName);
            }
        }
    }
}
