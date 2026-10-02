using MongoDB.Driver;

namespace Shared.DataAccess.Mongo
{
    /// <summary>
    /// Dựng <see cref="IMongoDatabase"/> dùng chung — sẵn sàng cho AuthService khi thực sự cần Mongo
    /// (VD login history/audit log), chưa wiring vào AuthService DI vì hiện chưa có nhu cầu dùng
    /// (roadmap-auth-sso.md).
    /// </summary>
    public static class MongoDatabaseFactory
    {
        public static IMongoDatabase Create(string connectionString, string databaseName)
        {
            var client = new MongoClient(connectionString);
            return client.GetDatabase(databaseName);
        }
    }
}
