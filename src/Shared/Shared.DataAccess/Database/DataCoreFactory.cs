using Shared.DataAccess.Abstractions;
using Shared.DataAccess.Oracle;
using Shared.DataAccess.Postgres;
using Shared.DataAccess.SqlServer;

namespace Shared.DataAccess.Database
{
    /// <summary>
    /// Tách riêng khỏi DependencyInjection.cs để test được độc lập (không cần dựng cả DI container
    /// + config Mongo/Redis chỉ để verify đúng provider được chọn).
    /// </summary>
    public static class DataCoreFactory
    {
        public static IDataCore Create(string connectionString)
        {
            var provider = ConnectionStringDetector.Detect(connectionString);
            return provider switch
            {
                DbProvider.Postgres => new PostgresDbHelper(connectionString),
                DbProvider.Oracle => new OracleDbHelper(connectionString),
                DbProvider.SqlServer => new SqlServerDbHelper(connectionString),
                _ => throw new NotSupportedException($"Provider {provider} chưa được hỗ trợ.")
            };
        }
    }
}
