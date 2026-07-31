using ApiCore8.Midleware;
using BLL;
using Core;
using Core.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ML;
using MongoDB.Bson;
using Serilog;
using System.Data;
using System.Dynamic;

namespace ApiCore8.Controllers
{
    [ApiController]
    [Route("api/[controller]/")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IBLL_ApiLogRepository _logRepository;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }
        [LogApi]
        [HttpGet("GetWeatherForecastf")]
        public async Task<dynamic> GetWeatherForecast()
        {
            //LogHelper.LogInformation("Ứng dụng khởi động thành công!");
            try
            {
                //Gửi mail
                bool result = EmailService.SendEmail(
                    smtpHost: "mail.dienmayxanh.com",
                    smtpPort: 587,
                    smtpUser: "hieu.nguyentrung@dienmayxanh.com",
                    smtpPassword: "#hhUKRum9T",
                    fromEmail: "hieu.nguyentrung@dienmayxanh.com",
                    fromName: "Hệ thống HieuPM",
                    toEmail: "hieupromen2011@gmail.com",
                    subject: "Test email",
                    body: "<h1>Xin chào</h1><p>Đây là email test.</p>",
                    isBodyHtml: true
                );
                string strconn = ConfigHelper.GetConnectionString("Postgres_wms");
                var db = new PostgresDbHelper(strconn);
                await db.StartTransactionScopeAsync();
                //db.AddParameter("@v_username", "hpm");
                //db.AddParameter("@v_email", "hpm@gmail.com");
                //db.AddParameter("@v_password", "admin");
                //db.AddParameter("@v_name", "admin");
                //int i = await db.ExecuteNonQueryAsync("public.user_getbyname");
                DataTable dataTable = await db.ExecuteStoreDataTableAsync("wms.cms_brand_getall");
                var users = new List<object>();
                //foreach (DataRow row in dataTable.Rows)
                //{
                //    users.Add(new
                //    {
                //        Id = row["id"].ToString(),
                //        Name = row["name"].ToString(),
                //        Email = row["email"].ToString()
                //    });
                //}
                await db.CommitTransactionAsync();
                return Ok(Summaries.ToList());
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }

        }

        //[LogApi]
        //[HttpPost("SearchLog")]
        //public async Task<APIResult> SearchLog([FromBody] LogFilterRequest request)
        //{
        //    APIResult objAPIResult = new APIResult();
        //    ResultMessage resultMessage = new ResultMessage();
        //    try
        //    {
        //        // Xử lý request và trả về kết quả
        //        PagedResult<ApiExecutionLog> pagedResult = new PagedResult<ApiExecutionLog>();
        //        pagedResult = await _logRepository.Search(request);
        //        objAPIResult.ResultObject = pagedResult;
        //        request.ApiName = "test";
        //        return objAPIResult;
        //    }
        //    catch (Exception ex)
        //    {
        //        return new APIResult(true,ResultMessage.ErrorTypes.LoadInfo,"Lỗi thực thi tìm kiểm log", ex.CreateExceptionMessage());
        //    }
        //}
        //[LogApi]
        //[HttpPost("GetlogByID")]
        //public async Task<APIResult> GetlogByID([FromBody] LogFilterRequest request)
        //{
        //    APIResult objAPIResult = new APIResult();
        //    ResultMessage resultMessage = new ResultMessage();
        //    ApiExecutionLog objApiExecutionLog = new ApiExecutionLog();

        //    try
        //    {
        //        if (request == null || string.IsNullOrWhiteSpace(request.Id))
        //        {
        //            return new APIResult(true, ResultMessage.ErrorTypes.LoadInfo, "Invalid request", "Id is required.");
        //        }

        //        if (!ObjectId.TryParse(request.Id, out var objectId))
        //        {
        //            return new APIResult(true, ResultMessage.ErrorTypes.LoadInfo, "Invalid request", "Id is not a valid ObjectId.");
        //        }
        //        // Xử lý request và trả về kết quả
        //        (objApiExecutionLog, resultMessage) = await _logRepository.GetLogByID(request.Id);
        //        if (resultMessage.IsError)
        //        {
        //            return new APIResult(true, ResultMessage.ErrorTypes.LoadInfo, "Lỗi thực thi tìm kiểm log", resultMessage.MessageDetail);
        //        }
        //        objAPIResult.ResultObject = objApiExecutionLog;
        //        return objAPIResult;
        //    }
        //    catch (Exception ex)
        //    {
        //        return new APIResult(true, ResultMessage.ErrorTypes.LoadInfo, "Lỗi thực thi tìm kiểm log", ex.CreateExceptionMessage());
        //    }
        //}
    }
}
