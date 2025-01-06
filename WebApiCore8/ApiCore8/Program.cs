using ApiCore8.Midleware;
using Core;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Sinks.Graylog;

internal class Program
{
    private static void Main(string[] args)
    {
        WebApplication app = null;
        var MyAllowSpecificOrigins = "CorsPolicy";
        string templateLog = "{Title}. {Content}.";
        try
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.AddSerilog();

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            app = builder.Build();
            // Configure the HTTP request pipeline.
          
            if (app.Environment.IsProduction() == false)
            {
                object[] arrayLog = new object[] { "Môi trường chạy HPM Service", $"Environment : {app.Environment.EnvironmentName}" };
                Log.Logger.Information(templateLog, arrayLog);
                app.UseSwagger();
                app.UseSwaggerUI();
                app.UseSerilogRequestLogging();
            }
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();
            app.UseMiddleware<GlobalExceptionMiddleware>();

            app.MapControllers();
            //DefaultFilesOptions options = new DefaultFilesOptions();
            //options.DefaultFileNames.Clear();
            //options.DefaultFileNames.Add("index.html");
            //app.UseDefaultFiles(options);
            //app.UseStaticFiles();
            app.Run();
        }
        catch (Exception exception)
        {
            object[] arrayLog = new object[] { "Lỗi khi khởi chạy HPM Service", $"Lỗi build HPM, exception: {exception.CreateExceptionMessage()}" };
            Log.Logger.Error(templateLog, arrayLog);
        }
    }
}

