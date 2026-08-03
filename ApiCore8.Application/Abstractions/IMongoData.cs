using ApiCore8.Application.Contracts;
using MongoDB.Driver;

namespace ApiCore8.Application.Abstractions
{
    /// <summary>
    /// MongoDB Data Access Layer Interface
    /// Định nghĩa tất cả operations cơ bản cho MongoDB
    /// </summary>
    public interface IMongoData : IDisposable
    {
        #region Setup

        /// <summary>
        /// Kết nối tới MongoDB (idempotent - gọi nhiều lần cũng OK)
        /// </summary>
        void Connect();

        /// <summary>
        /// Set collection name để thao tác
        /// </summary>
        /// <param name="collectionName">Tên collection</param>
        void AddCollection(string collectionName);

        #endregion

        #region Read Operations (Query)

        /// <summary>
        /// Lấy danh sách documents theo filter
        /// </summary>
        Task<List<T>> Get<T>(FilterDefinition<T>? filter);

        /// <summary>
        /// Lấy 1 document đầu tiên
        /// </summary>
        Task<T?> GetOne<T>(FilterDefinition<T> filter);

        /// <summary>
        /// Lấy dữ liệu có phân trang (RECOMMENDED)
        /// </summary>
        Task<PagedResult<T>> GetPaged<T>(
            FilterDefinition<T> filter,
            int page,
            int pageSize,
            SortDefinition<T>? sort = null);

        /// <summary>
        /// Đếm số documents
        /// </summary>
        Task<int> CountAsync<T>(FilterDefinition<T>? filter = null);

        /// <summary>
        /// Find với options chi tiết
        /// </summary>
        Task<List<T>> FindAsync<T>(
            FilterDefinition<T>? filter = null,
            int limit = 0,
            SortDefinition<T>? sort = null,
            int skip = 0);

        #endregion

        #region Write Operations (Command)

        /// <summary>
        /// Insert 1 document
        /// </summary>
        Task<bool> Insert<T>(T obj);

        /// <summary>
        /// Insert nhiều documents
        /// </summary>
        Task<bool> InsertMany<T>(List<T> list);

        /// <summary>
        /// Update với dictionary
        /// </summary>
        Task<bool> Update<T>(
            FilterDefinition<T> filter,
            Dictionary<string, object> updateFields,
            bool isUpsert = false);

        /// <summary>
        /// Update với UpdateDefinition
        /// </summary>
        Task<bool> UpdateAsync<T>(
            FilterDefinition<T> filter,
            UpdateDefinition<T> updateDefinition,
            bool isUpsert = false);

        /// <summary>
        /// Update nhiều documents
        /// </summary>
        Task<long> UpdateManyAsync<T>(
            FilterDefinition<T> filter,
            UpdateDefinition<T> updateDefinition);

        /// <summary>
        /// Delete 1 document
        /// </summary>
        Task<bool> Delete<T>(FilterDefinition<T> filter);

        /// <summary>
        /// Delete nhiều documents
        /// </summary>
        Task<long> DeleteManyAsync<T>(FilterDefinition<T> filter);

        #endregion

        #region Index Operations

        /// <summary>
        /// Tạo indexes (idempotent)
        /// </summary>
        Task CreateIndex<T>(List<CreateIndexModel<T>> list);

        /// <summary>
        /// Xóa tất cả indexes
        /// </summary>
        Task DropAllIndex<T>();

        #endregion
    }
}