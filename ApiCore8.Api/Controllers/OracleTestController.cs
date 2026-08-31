using ApiCore8.Application.Abstractions;
using ApiCore8.Infrastructure.Oracle;
using Microsoft.AspNetCore.Mvc;

namespace ApiCore8.Api.Controllers
{
    /// <summary>Controller test CRUD với Oracle qua bảng "users". Đọc ConnectionStrings:Oracle.</summary>
    [ApiController]
    [Route("api/[controller]")]
    public class OracleTestController : DbTestControllerBase
    {
        private readonly IConfiguration _configuration;

        public OracleTestController(IConfiguration configuration, ILogger<OracleTestController> logger)
            : base(logger)
        {
            _configuration = configuration;
        }

        protected override IDataCore CreateDataCore()
        {
            var connectionString = _configuration.GetConnectionString("Oracle")
                ?? throw new InvalidOperationException("ConnectionStrings:Oracle not configured");
            return new OracleDbHelper(connectionString);
        }
    }
}
