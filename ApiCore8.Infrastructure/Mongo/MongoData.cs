using ApiCore8.Application.Abstractions;
using ApiCore8.Application.Contracts;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;

namespace ApiCore8.Infrastructure.Mongo
{
    /// <summary>
    /// MongoDB Data Access Layer - Generic Repository Pattern
    /// Thread-safe, production-ready implementation với Dual Logging
    /// </summary>
    public class MongoData : IMongoData, IDisposable
    {
        #region Private Fields
        
        private readonly string _databaseName;
        private readonly string _connectionString;
        private readonly MongoClient? _injectedClient;
        private readonly IMongoLoggerService? _mongoLogger; // ✅ Add MongoDB logger
        private string _collectionName = string.Empty;
        private MongoClient? _mongoClient;
        private IMongoDatabase? _database;
        private bool _ownsClient = false;
        private bool _disposed = false;
        private const string LOG_CATEGORY = "MongoData"; // ✅ Category constant

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor - Inject MongoClient và IMongoLoggerService
        /// </summary>
        /// <param name="client">MongoClient singleton (từ DI container)</param>
        /// <param name="connectionString">Connection string (optional nếu có client)</param>
        /// <param name="databaseName">Tên database (optional, lấy từ config nếu null)</param>
        /// <param name="mongoLogger">MongoDB logger service (optional)</param>
        public MongoData(
            MongoClient? client = null, 
            string connectionString = "", 
            string databaseName = "",
            IMongoLoggerService? mongoLogger = null) // ✅ Add parameter
        {
            _injectedClient = client;
            _mongoLogger = mongoLogger; // ✅ Save reference
            
            _connectionString = string.IsNullOrWhiteSpace(connectionString)
                ? ConfigHelper.GetConnectionString("MongoDB")
                : connectionString;
            
            _databaseName = string.IsNullOrWhiteSpace(databaseName)
                ? ConfigHelper.GetValue("Database:MongoDatabase")
                : databaseName;
        }

        #endregion

        #region Public Methods - Setup

        public void Connect()
        {
            if (_database != null)
                return;

            if (_injectedClient != null)
            {
                _mongoClient = _injectedClient;
                _ownsClient = false;
            }
            else
            {
                _mongoClient = new MongoClient(_connectionString);
                _ownsClient = true;
            }

            _database = _mongoClient.GetDatabase(_databaseName);
        }

        public void AddCollection(string collectionName)
        {
            if (string.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException("Collection name cannot be empty", nameof(collectionName));
            
            _collectionName = collectionName;
        }

        #endregion

        #region Private Helper

        private IMongoCollection<T> GetCollection<T>()
        {
            if (_database == null)
                Connect();

            if (_database == null)
                throw new InvalidOperationException("MongoDB database is not initialized");

            if (string.IsNullOrWhiteSpace(_collectionName))
                throw new InvalidOperationException("Collection name is not set. Call AddCollection() first");

            return _database.GetCollection<T>(_collectionName);
        }

        #endregion

        #region Read Operations (Query)

        /// <summary>
        /// Lấy danh sách documents theo filter
        /// </summary>
        public async Task<List<T>> Get<T>(FilterDefinition<T>? filter)
        {
            try
            {
                filter ??= Builders<T>.Filter.Empty;
                return await GetCollection<T>().Find(filter).ToListAsync();
            }
            catch (Exception ex)
            {
                // ✅ DUAL LOGGING
                Log.Error(ex, "Error in MongoData.Get for collection {Collection}", _collectionName);
                _mongoLogger?.LogError(LOG_CATEGORY, 
                    $"Error in Get for collection {_collectionName}", ex,
                    new { Collection = _collectionName });
                throw;
            }
        }

        /// <summary>
        /// Lấy 1 document đầu tiên theo filter
        /// </summary>
        public async Task<T?> GetOne<T>(FilterDefinition<T> filter)
        {
            try
            {
                return await GetCollection<T>().Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                // ✅ DUAL LOGGING
                Log.Error(ex, "Error in MongoData.GetOne for collection {Collection}", _collectionName);
                _mongoLogger?.LogError(LOG_CATEGORY, 
                    $"Error in GetOne for collection {_collectionName}", ex,
                    new { Collection = _collectionName });
                throw;
            }
        }

        /// <summary>
        /// Lấy dữ liệu có phân trang (RECOMMENDED cho UI)
        /// </summary>
        public async Task<PagedResult<T>> GetPaged<T>(
            FilterDefinition<T> filter,
            int page,
            int pageSize,
            SortDefinition<T>? sort = null)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > 1000) pageSize = 1000;

                var collection = GetCollection<T>();
                sort ??= Builders<T>.Sort.Descending("_id");

                var countTask = collection.CountDocumentsAsync(filter);
                var skip = (page - 1) * pageSize;
                var itemsTask = collection
                    .Find(filter)
                    .Sort(sort)
                    .Skip(skip)
                    .Limit(pageSize)
                    .ToListAsync();

                await Task.WhenAll(countTask, itemsTask);

                return new PagedResult<T>
                {
                    Items = itemsTask.Result,
                    Total = (int)countTask.Result,
                    Page = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                // ✅ DUAL LOGGING
                Log.Error(ex, "Error in MongoData.GetPaged for collection {Collection}", _collectionName);
                _mongoLogger?.LogError(LOG_CATEGORY, 
                    $"Error in GetPaged for collection {_collectionName}", ex,
                    new { Collection = _collectionName, Page = page, PageSize = pageSize });
                throw;
            }
        }

        /// <summary>
        /// Đếm số documents theo filter
        /// </summary>
        public async Task<int> CountAsync<T>(FilterDefinition<T>? filter = null)
        {
            try
            {
                filter ??= Builders<T>.Filter.Empty;
                var count = await GetCollection<T>().CountDocumentsAsync(filter);
                return (int)count;
            }
            catch (Exception ex)
            {
                // ✅ DUAL LOGGING
                Log.Error(ex, "Error in MongoData.CountAsync for collection {Collection}", _collectionName);
                _mongoLogger?.LogError(LOG_CATEGORY, 
                    $"Error in CountAsync for collection {_collectionName}", ex,
                    new { Collection = _collectionName });
                throw;
            }
        }

        /// <summary>
        /// Find với options chi tiết (limit, sort, skip)
        /// </summary>
        public async Task<List<T>> FindAsync<T>(
            FilterDefinition<T>? filter = null,
            int limit = 0,
            SortDefinition<T>? sort = null,
            int skip = 0)
        {
            try
            {
                filter ??= Builders<T>.Filter.Empty;
                var query = GetCollection<T>().Find(filter);

                if (sort != null)
                    query = query.Sort(sort);

                if (skip > 0)
                    query = query.Skip(skip);

                if (limit > 0)
                    query = query.Limit(limit);

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                // ✅ DUAL LOGGING
                Log.Error(ex, "Error in MongoData.FindAsync for collection {Collection}", _collectionName);
                _mongoLogger?.LogError(LOG_CATEGORY, 
                    $"Error in FindAsync for collection {_collectionName}", ex,
                    new { Collection = _collectionName, Limit = limit, Skip = skip });
                throw;
            }
        }

        #endregion

        #region Write Operations (Command)

        /// <summary>
        /// Insert 1 document
        /// </summary>
        public async Task<bool> Insert<T>(T entity)
        {
            try
            {
                await GetCollection<T>().InsertOneAsync(entity);
                return true;
            }
            catch (Exception ex)
            {
                // ✅ DUAL LOGGING
                Log.Error(ex, "Error in MongoData.Insert for collection {Collection}", _collectionName);
                _mongoLogger?.LogError(LOG_CATEGORY, 
                    $"Error in Insert for collection {_collectionName}", ex,
                    new { Collection = _collectionName });
                return false;
            }
        }

        /// <summary>
        /// Insert nhiều documents (bulk insert)
        /// </summary>
        public async Task<bool> InsertMany<T>(List<T> entities)
        {
            try
            {
                if (entities == null || entities.Count == 0)
                    return true;

                await GetCollection<T>().InsertManyAsync(entities);
                return true;
            }
            catch (Exception ex)
            {
                // ✅ DUAL LOGGING
                Log.Error(ex, "Error in MongoData.InsertMany for collection {Collection}", _collectionName);
                _mongoLogger?.LogError(LOG_CATEGORY, 
                    $"Error in InsertMany for collection {_collectionName}", ex,
                    new { Collection = _collectionName, Count = entities?.Count ?? 0 });
                return false;
            }
        }

        /// <summary>
        /// Update document với dictionary (field-value pairs)
        /// </summary>
        public async Task<bool> Update<T>(
            FilterDefinition<T> filter,
            Dictionary<string, object> updateFields,
            bool isUpsert = false)
        {
            try
            {
                if (updateFields == null || updateFields.Count == 0)
                    return false;

                var firstField = updateFields.First();
                var updateDef = Builders<T>.Update.Set(firstField.Key, firstField.Value);

                foreach (var field in updateFields.Skip(1))
                {
                    updateDef = updateDef.Set(field.Key, field.Value);
                }

                var options = new UpdateOptions { IsUpsert = isUpsert };
                var result = await GetCollection<T>().UpdateOneAsync(filter, updateDef, options);

                return result.ModifiedCount > 0 || (isUpsert && result.UpsertedId != null);
            }
            catch (Exception ex)
            {
                // ✅ DUAL LOGGING
                Log.Error(ex, "Error in MongoData.Update for collection {Collection}", _collectionName);
                _mongoLogger?.LogError(LOG_CATEGORY, 
                    $"Error in Update for collection {_collectionName}", ex,
                    new { Collection = _collectionName, IsUpsert = isUpsert });
                return false;
            }
        }

        /// <summary>
        /// Update với UpdateDefinition (advanced)
        /// </summary>
        public async Task<bool> UpdateAsync<T>(
            FilterDefinition<T> filter,
            UpdateDefinition<T> updateDefinition,
            bool isUpsert = false)
        {
            try
            {
                var options = new UpdateOptions { IsUpsert = isUpsert };
                var result = await GetCollection<T>().UpdateOneAsync(filter, updateDefinition, options);

                return result.ModifiedCount > 0 || (isUpsert && result.UpsertedId != null);
            }
            catch (Exception ex)
            {
                // ✅ DUAL LOGGING
                Log.Error(ex, "Error in MongoData.UpdateAsync for collection {Collection}", _collectionName);
                _mongoLogger?.LogError(LOG_CATEGORY, 
                    $"Error in UpdateAsync for collection {_collectionName}", ex,
                    new { Collection = _collectionName, IsUpsert = isUpsert });
                return false;
            }
        }

        /// <summary>
        /// Update nhiều documents
        /// </summary>
        public async Task<long> UpdateManyAsync<T>(
            FilterDefinition<T> filter,
            UpdateDefinition<T> updateDefinition)
        {
            try
            {
                var result = await GetCollection<T>().UpdateManyAsync(filter, updateDefinition);
                return result.ModifiedCount;
            }
            catch (Exception ex)
            {
                // ✅ DUAL LOGGING
                Log.Error(ex, "Error in MongoData.UpdateManyAsync for collection {Collection}", _collectionName);
                _mongoLogger?.LogError(LOG_CATEGORY, 
                    $"Error in UpdateManyAsync for collection {_collectionName}", ex,
                    new { Collection = _collectionName });
                return 0;
            }
        }

        /// <summary>
        /// Delete 1 document
        /// </summary>
        public async Task<bool> Delete<T>(FilterDefinition<T> filter)
        {
            try
            {
                var result = await GetCollection<T>().DeleteOneAsync(filter);
                return result.DeletedCount > 0;
            }
            catch (Exception ex)
            {
                // ✅ DUAL LOGGING
                Log.Error(ex, "Error in MongoData.Delete for collection {Collection}", _collectionName);
                _mongoLogger?.LogError(LOG_CATEGORY, 
                    $"Error in Delete for collection {_collectionName}", ex,
                    new { Collection = _collectionName });
                return false;
            }
        }

        /// <summary>
        /// Delete nhiều documents
        /// </summary>
        public async Task<long> DeleteManyAsync<T>(FilterDefinition<T> filter)
        {
            try
            {
                var result = await GetCollection<T>().DeleteManyAsync(filter);
                return result.DeletedCount;
            }
            catch (Exception ex)
            {
                // ✅ DUAL LOGGING
                Log.Error(ex, "Error in MongoData.DeleteManyAsync for collection {Collection}", _collectionName);
                _mongoLogger?.LogError(LOG_CATEGORY, 
                    $"Error in DeleteManyAsync for collection {_collectionName}", ex,
                    new { Collection = _collectionName });
                return 0;
            }
        }

        #endregion

        #region Index Operations

        /// <summary>
        /// Tạo indexes (idempotent - chỉ tạo nếu chưa tồn tại)
        /// </summary>
        public async Task CreateIndex<T>(List<CreateIndexModel<T>> indexModels)
        {
            if (indexModels == null || indexModels.Count == 0)
                return;

            try
            {
                var collection = GetCollection<T>();
                var existingCursor = await collection.Indexes.ListAsync();
                var existingIndexes = await existingCursor.ToListAsync();
                
                var existingNames = existingIndexes
                    .Where(d => d.Contains("name"))
                    .Select(d => d["name"].AsString)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var indexesToCreate = new List<CreateIndexModel<T>>();

                foreach (var model in indexModels)
                {
                    var indexName = model.Options?.Name;

                    if (!string.IsNullOrEmpty(indexName))
                    {
                        if (!existingNames.Contains(indexName))
                        {
                            indexesToCreate.Add(model);
                        }
                        continue;
                    }

                    try
                    {
                        var renderedKeys = model.Keys.ToBsonDocument();
                        bool found = existingIndexes.Any(d =>
                            d.Contains("key") &&
                            d["key"].AsBsonDocument.Equals(renderedKeys)
                        );

                        if (!found)
                        {
                            indexesToCreate.Add(model);
                        }
                    }
                    catch
                    {
                        indexesToCreate.Add(model);
                    }
                }

                if (indexesToCreate.Count > 0)
                {
                    try
                    {
                        await collection.Indexes.CreateManyAsync(indexesToCreate);
                        
                        // ✅ DUAL LOGGING for success
                        Log.Information("Created {Count} indexes for collection {Collection}", 
                            indexesToCreate.Count, _collectionName);
                        _mongoLogger?.LogInformation(LOG_CATEGORY,
                            $"Created {indexesToCreate.Count} indexes for collection {_collectionName}",
                            new { Collection = _collectionName, IndexCount = indexesToCreate.Count });
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Failed to batch create indexes, falling back to individual creation");
                        _mongoLogger?.LogWarning(LOG_CATEGORY,
                            "Failed to batch create indexes, falling back to individual creation",
                            new { Collection = _collectionName });
                        
                        foreach (var model in indexesToCreate)
                        {
                            try
                            {
                                await collection.Indexes.CreateOneAsync(model);
                            }
                            catch
                            {
                                // Swallow individual index creation errors
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // ✅ DUAL LOGGING
                Log.Error(ex, "Error in MongoData.CreateIndex for collection {Collection}", _collectionName);
                _mongoLogger?.LogError(LOG_CATEGORY,
                    $"Error in CreateIndex for collection {_collectionName}", ex,
                    new { Collection = _collectionName });
            }
        }

        /// <summary>
        /// Xóa tất cả indexes (trừ _id index)
        /// </summary>
        public async Task DropAllIndex<T>()
        {
            try
            {
                await GetCollection<T>().Indexes.DropAllAsync();
            }
            catch (Exception ex)
            {
                // ✅ DUAL LOGGING
                Log.Error(ex, "Error in MongoData.DropAllIndex for collection {Collection}", _collectionName);
                _mongoLogger?.LogError(LOG_CATEGORY,
                    $"Error in DropAllIndex for collection {_collectionName}", ex,
                    new { Collection = _collectionName });
                throw;
            }
        }

        #endregion

        #region IDisposable Pattern

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                if (_ownsClient && _mongoClient != null)
                {
                    try
                    {
                        (_mongoClient as IDisposable)?.Dispose();
                    }
                    catch
                    {
                        // Swallow disposal errors
                    }
                }
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
