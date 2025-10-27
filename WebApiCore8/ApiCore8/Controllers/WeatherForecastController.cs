using ApiCore8.Midleware;
using Core;
using Core.Database;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Data;
using System.Dynamic;

namespace ApiCore8.Controllers
{
    [ApiController]
    [TypeFilter(typeof(LogApiAttribute))] // Gắn attribute tại đây
    [Route("api/[controller]/")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly PostgresDbHelper _db;
        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
            //_db = objPostgresDbHelper;
        }

        [HttpGet("GetWeatherForecastf")]
        public async Task<dynamic> GetWeatherForecast()
        {
            //LogHelper.LogInformation("Ứng dụng khởi động thành công!");
            try
            {
                var db = new PostgresDbHelper();
                await db.StartTransactionScopeAsync();
                //db.AddParameter("@v_username", "hpm");
                //db.AddParameter("@v_email", "hpm@gmail.com");
                //db.AddParameter("@v_password", "admin");
                db.AddParameter("@v_name", "admin");
                //int i = await db.ExecuteNonQueryAsync("public.user_getbyname");
                DataTable dataTable = await db.ExecuteStoreDataTableAsync("public.user_getbyname");
                var users = new List<object>();
                foreach (DataRow row in dataTable.Rows)
                {
                    users.Add(new
                    {
                        Id = row["id"].ToString(),
                        Name = row["name"].ToString(),
                        Email = row["email"].ToString()
                    });
                }
                await db.CommitTransactionAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }

        }
    }
}
