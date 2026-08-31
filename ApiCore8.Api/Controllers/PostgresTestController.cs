using ApiCore8.Application.Abstractions;
using ApiCore8.Infrastructure.Postgres;
using Microsoft.AspNetCore.Mvc;

namespace ApiCore8.Api.Controllers
{
    /// <summary>Controller test CRUD với Postgres qua bảng "users". Đọc ConnectionStrings:Postgres.</summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PostgresTestController : DbTestControllerBase
    {
        private readonly IConfiguration _configuration;

        public PostgresTestController(IConfiguration configuration, ILogger<PostgresTestController> logger)
            : base(logger)
        {
            _configuration = configuration;
        }

        protected override IDataCore CreateDataCore()
        {
            var connectionString = _configuration.GetConnectionString("Postgres")
                ?? throw new InvalidOperationException("ConnectionStrings:Postgres not configured");
            return new PostgresDbHelper(connectionString);
        }
    }
}
