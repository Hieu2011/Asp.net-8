using StackExchange.Redis;

namespace Shared.Caching.Redis
{
    /// <summary>
    /// Dựng <see cref="IConnectionMultiplexer"/> dùng chung giữa ApiCore8/AuthService — mỗi service
    /// tự quyết định lifetime (Singleton/Lazy) và cách đăng ký DI khi dùng kết quả trả về.
    /// </summary>
    public static class RedisConnectionFactory
    {
        public static IConnectionMultiplexer Connect(string connectionString)
            => ConnectionMultiplexer.Connect(connectionString);
    }
}
