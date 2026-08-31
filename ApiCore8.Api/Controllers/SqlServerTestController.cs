using ApiCore8.Application.Abstractions;
using ApiCore8.Infrastructure.SqlServer;
using Microsoft.AspNetCore.Mvc;

namespace ApiCore8.Api.Controllers
{
    /// <summary>Controller test CRUD với SQL Server qua bảng "users". Đọc ConnectionStrings:SqlServer.</summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SqlServerTestController : DbTestControllerBase
    {
        private readonly IConfiguration _configuration;

        public SqlServerTestController(IConfiguration configuration, ILogger<SqlServerTestController> logger)
            : base(logger)
        {
            _configuration = configuration;
        }

        protected override IDataCore CreateDataCore()
        {
            var connectionString = _configuration.GetConnectionString("SqlServer")
                ?? throw new InvalidOperationException("ConnectionStrings:SqlServer not configured");
            return new SqlServerDbHelper(connectionString);
        }
    }
}
