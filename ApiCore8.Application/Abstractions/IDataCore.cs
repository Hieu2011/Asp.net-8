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
        Task<T> ExecStoreToObjectAsync<T>(string storeName, CancellationToken cancellationToken = default);
        Task<int> ExecuteNonQueryAsync(string storeName, CancellationToken cancellationToken = default);
        Task<string> ExecuteNonQueryAsStringAsync(string storeName, CancellationToken cancellationToken = default);
        Task<DataTable> ExecuteStoreDataTableAsync(string storeName, CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
        Task StartTransactionScopeAsync(CancellationToken cancellationToken = default);
    }
}