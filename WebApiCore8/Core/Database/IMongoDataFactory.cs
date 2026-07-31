namespace Core.Database
{
    /// <summary>
    /// Factory pattern để tạo MongoData instances
    /// Hỗ trợ multiple databases/collections và centralized configuration
    /// </summary>
    public interface IMongoDataFactory
    {
        /// <summary>
        /// Tạo MongoData với default config từ appsettings.json
        /// Database: "Database:MongoDatabase"
        /// Collection: "Database:MongoCollection"
        /// </summary>
        /// <returns>MongoData instance đã kết nối và set collection</returns>
        IMongoData Create();

        /// <summary>
        /// Tạo MongoData với custom collection name
        /// Sử dụng default database từ config
        /// </summary>
        /// <param name="collectionName">Tên collection</param>
        /// <returns>MongoData instance đã kết nối và set collection</returns>
        IMongoData Create(string collectionName);

        /// <summary>
        /// Tạo MongoData với custom connection string, database, collection
        /// Dùng khi cần kết nối tới database khác
        /// </summary>
        /// <param name="connectionString">MongoDB connection string</param>
        /// <param name="databaseName">Database name</param>
        /// <param name="collectionName">Collection name</param>
        /// <returns>MongoData instance đã kết nối và set collection</returns>
        IMongoData Create(string connectionString, string databaseName, string collectionName);
    }
}