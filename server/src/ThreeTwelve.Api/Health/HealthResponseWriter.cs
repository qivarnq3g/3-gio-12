using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ThreeTwelve.Api.Health;

public static class HealthResponseWriter
{
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        var status = report.Status == HealthStatus.Healthy ? "ok" : "degraded";
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(new { status }));
    }
}
