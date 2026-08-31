using ApiCore8.Application.Contracts;
using MongoDB.Driver;

namespace ApiCore8.Application.Extensions
{
    /// <summary>
    /// Convenience helpers on top of the native MongoDB.Driver IMongoCollection&lt;T&gt; —
    /// no custom repository wrapper needed, just the one bit MongoDB.Driver doesn't
    /// provide out of the box: filter + sort + skip/limit + count combined into a PagedResult.
    /// </summary>
    public static class MongoCollectionExtensions
    {
        public static async Task<PagedResult<T>> GetPagedAsync<T>(
            this IMongoCollection<T> collection,
            FilterDefinition<T> filter,
            int page,
            int pageSize,
            SortDefinition<T>? sort = null,
            CancellationToken cancellationToken = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 1000) pageSize = 1000;

            sort ??= Builders<T>.Sort.Descending("_id");

            var countTask = collection.CountDocumentsAsync(filter, options: null, cancellationToken: cancellationToken);
            var skip = (page - 1) * pageSize;
            var itemsTask = collection
                .Find(filter)
                .Sort(sort)
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync(cancellationToken);

            await Task.WhenAll(countTask, itemsTask);

            return new PagedResult<T>
            {
                Items = itemsTask.Result,
                Total = (int)countTask.Result,
                Page = page,
                PageSize = pageSize
            };
        }
    }
}
