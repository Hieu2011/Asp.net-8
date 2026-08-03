
namespace ApiCore8.Application.Abstractions
{
    public interface IMongoLoggerService
    {
        void Dispose();
        Task EnsureIndexesAsync();
        Task FlushAsync();
        void LogCritical(string category, string message, Exception? exception = null, object? data = null);
        void LogDebug(string category, string message, object? data = null);
        void LogError(string category, string message, Exception? exception = null, object? data = null);
        void LogInformation(string category, string message, object? data = null);
        void LogWarning(string category, string message, object? data = null);
    }
}