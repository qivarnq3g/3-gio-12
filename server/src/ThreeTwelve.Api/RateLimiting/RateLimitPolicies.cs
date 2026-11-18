using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace ThreeTwelve.Api.RateLimiting;

public static class RateLimitPolicies
{
    public const string Telemetry = "telemetry";
    public const string PuzzleSubmit = "puzzle-submit";

    public static IServiceCollection AddPublicRateLimits(this IServiceCollection services, IConfiguration configuration)
    {
        var telemetryLimit = configuration.GetValue("RateLimits:Telemetry:PermitLimit", 60);
        var puzzleSubmitLimit = configuration.GetValue("RateLimits:PuzzleSubmit:PermitLimit", 20);

        return services.AddRateLimiter(options =>
        {
            options.AddPolicy(Telemetry, context => PerAddressPerMinute(context, telemetryLimit));
            options.AddPolicy(PuzzleSubmit, context => PerAddressPerMinute(context, puzzleSubmitLimit));

            options.OnRejected = async (context, ct) =>
            {
                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var wait)
                    ? Math.Max(1, (int)Math.Ceiling(wait.TotalSeconds))
                    : 60;
                var response = context.HttpContext.Response;
                response.StatusCode = StatusCodes.Status429TooManyRequests;
                response.Headers.RetryAfter = retryAfter.ToString(CultureInfo.InvariantCulture);
                await response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Gửi quá nhanh",
                    Detail = $"Thử lại sau {retryAfter} giây.",
                }, options: null, contentType: "application/problem+json", ct);
            };
        });
    }

    private static RateLimitPartition<string> PerAddressPerMinute(HttpContext context, int permitLimit) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            });
}
