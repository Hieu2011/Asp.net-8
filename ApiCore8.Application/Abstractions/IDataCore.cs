using System.Data;

namespace ApiCore8.Application.Abstractions
{
    public interface IDataCore : IDisposable
    {
        IDbConnection IConnection
        {
            get;
            set;
        }

        IDbCommand ICommand
        {
            get;
            set;
        }

        IDbTransaction ITransaction
        {
            get;
            set;
        }
        void AddParameter(string paramName, object value);
        void ClearParameters();
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task<List<T>> ExecStoreToListObjectAsync<T>(string storeName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Y hệt ExecStoreToListObjectAsync về cách dùng (tự động map, không cần viết tay) nhưng
        /// đọc thẳng IDataReader, KHÔNG qua DataTable — nhanh hơn ~7-8 lần ở quy mô chục-trăm nghìn
        /// dòng (đã benchmark). Thêm để so sánh song song với bản cũ, chưa thay thế.
        /// </summary>
        Task<List<T>> ExecStoreToListObjectFastAsync<T>(string storeName, CancellationToken cancellationToken = default);
        Task<T> ExecStoreToObjectAsync<T>(string storeName, CancellationToken cancellationToken = default);
        Task<int> ExecuteNonQueryAsync(string storeName, CancellationToken cancellationToken = default);
        Task<string> ExecuteNonQueryAsStringAsync(string storeName, CancellationToken cancellationToken = default);
        Task<DataTable> ExecuteStoreDataTableAsync(string storeName, CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
        Task StartTransactionScopeAsync(CancellationToken cancellationToken = default);
    }
}