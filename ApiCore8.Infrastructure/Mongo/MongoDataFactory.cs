using ApiCore8.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace ApiCore8.Infrastructure.Mongo
{
    /// <summary>
    /// Factory implementation để tạo MongoData instances
    /// Thread-safe, singleton pattern với MongoDB logging support
    /// </summary>
    public class MongoDataFactory : IMongoDataFactory
    {
        private readonly MongoClient _client;
        private readonly IConfiguration _configuration;
        private readonly IMongoLoggerService? _mongoLogger; // ✅ Add this

        /// <summary>
        /// Constructor - Inject MongoClient, IConfiguration và IMongoLoggerService
        /// </summary>
        public MongoDataFactory(
            MongoClient client, 
            IConfiguration configuration,
            IMongoLoggerService? mongoLogger = null) // ✅ Add parameter
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _mongoLogger = mongoLogger; // ✅ Save reference
        }

        /// <summary>
        /// Tạo MongoData với default config từ appsettings.json
        /// </summary>
        public IMongoData Create()
        {
            string connectionString = _configuration.GetConnectionString("MongoDB") 
                ?? throw new InvalidOperationException("MongoDB connection string not found in configuration. Add 'ConnectionStrings:MongoDB' to appsettings.json");
            
            string databaseName = _configuration["Database:MongoDatabase"] 
                ?? throw new InvalidOperationException("MongoDB database name not found in configuration. Add 'Database:MongoDatabase' to appsettings.json");
            
            string collectionName = _configuration["Database:MongoCollection"] 
                ?? throw new InvalidOperationException("MongoDB collection name not found in configuration. Add 'Database:MongoCollection' to appsettings.json");

            // ✅ Pass mongoLogger to MongoData
            var mongo = new MongoData(_client, connectionString, databaseName, _mongoLogger);
            mongo.AddCollection(collectionName);
            mongo.Connect();
            
            return mongo;
        }

        /// <summary>
        /// Tạo MongoData với custom collection name
        /// </summary>
        public IMongoData Create(string collectionName)
        {
            if (string.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException("Collection name cannot be null or empty", nameof(collectionName));

            string connectionString = _configuration.GetConnectionString("MongoDB") 
                ?? throw new InvalidOperationException("MongoDB connection string not found in configuration");
            
            string databaseName = _configuration["Database:MongoDatabase"] 
                ?? throw new InvalidOperationException("MongoDB database name not found in configuration");

            // ✅ Pass mongoLogger to MongoData
            var mongo = new MongoData(_client, connectionString, databaseName, _mongoLogger);
            mongo.AddCollection(collectionName);
            mongo.Connect();
            
            return mongo;
        }

        /// <summary>
        /// Tạo MongoData với full custom parameters
        /// </summary>
        public IMongoData Create(string connectionString, string databaseName, string collectionName)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));
            
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Database name cannot be null or empty", nameof(databaseName));
            
            if (string.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException("Collection name cannot be null or empty", nameof(collectionName));

            // ✅ Pass mongoLogger to MongoData
            var mongo = new MongoData(_client, connectionString, databaseName, _mongoLogger);
            mongo.AddCollection(collectionName);
            mongo.Connect();
            
            return mongo;
        }
    }
}
