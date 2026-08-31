using ApiCore8.Application.Abstractions;
using ApiCore8.Infrastructure.Oracle;
using ApiCore8.Infrastructure.Postgres;
using ApiCore8.Infrastructure.SqlServer;

namespace ApiCore8.Infrastructure.Database
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
