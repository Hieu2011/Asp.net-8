using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Shared.Common.Health;

/// <summary>Healthcheck dùng chung: endpoint <c>/health</c> + chế độ <c>--healthcheck</c> cho Coolify.</summary>
public static class HealthCheckProbe
{
    public const string Path = "/health";
    public const string Argument = "--healthcheck";

    public static bool IsProbeMode(string[] arrArgs) => arrArgs.Contains(Argument);

    public static bool IsHealthRequest(HttpContext httpContext) => httpContext.Request.Path.StartsWithSegments(Path);

    public static IEndpointConventionBuilder MapHealthProbe(this IEndpointRouteBuilder endpoints) => endpoints.MapHealthChecks(Path);

    /// <summary>Gọi /health của app đang chạy trong container — Coolify (Container command) chặn toán tử shell, image không có curl.</summary>
    public static async Task<int> RunAsync()
    {
        string strPort = (Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS") ?? "8080").Split(';', ',')[0];
        try
        {
            using HttpClient objHttpClient = new() { Timeout = TimeSpan.FromSeconds(3) };
            using HttpResponseMessage objResponse = await objHttpClient.GetAsync($"http://127.0.0.1:{strPort}{Path}");
            return objResponse.IsSuccessStatusCode ? 0 : 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }
}
