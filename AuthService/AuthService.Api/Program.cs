using AuthService.Api;
using Serilog;

internal class Program
{
    private static async Task Main(string[] args)
    {
        WebApplication app = null!;
        try
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseSerilog((context, configuration) =>
            {
                configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .Enrich.FromLogContext()
                    .WriteTo.Console();
            });

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddAppServices(builder.Configuration);

            app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSerilogRequestLogging();

            app.MapControllers();

            await app.RunAsync();
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}
