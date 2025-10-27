using System.Data;

namespace Core.Database
{
    public interface IData
    {
        IDbConnection IConnection { get; set; }
        IDbTransaction ITransaction { get; set; }
        IDbCommand ICommand { get; set; }

        void Connect();
        void Disconnect();
        void ExecUpdate(string v);
    }
}