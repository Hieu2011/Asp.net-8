using System.Data;

namespace Core.Database
{
    public interface IDataCore
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
        Task CommitTransactionAsync();
        void Dispose();
        Task<List<T>> ExecStoreToListObjectAsync<T>(string storeName);
        Task<T> ExecStoreToObjectAsync<T>(string storeName);
        Task<int> ExecuteNonQueryAsync(string storeName);
        Task<DataTable> ExecuteStoreDataTableAsync(string storeName);
        Task RollbackTransactionAsync();
        Task StartTransactionScopeAsync();
    }
}