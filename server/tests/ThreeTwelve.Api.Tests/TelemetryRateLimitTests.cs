using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;

namespace ThreeTwelve.Api.Tests;

public sealed class TightRateLimitApiFactory : PostgresApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("RateLimits:Telemetry:PermitLimit", "2");
    }
}

public sealed class TelemetryRateLimitTests(TightRateLimitApiFactory factory) : IClassFixture<TightRateLimitApiFactory>
{
    [Fact]
    public async Task Answers_429_with_retry_after_once_the_limit_is_used_up()
    {
        using var client = factory.CreateClient();
        var batch = TelemetryPayloads.Batch(Guid.NewGuid(), TelemetryPayloads.SessionStarted());

        await client.PostAsJsonAsync("/api/v1/telemetry/batches", batch);
        await client.PostAsJsonAsync("/api/v1/telemetry/batches", batch);
        var third = await client.PostAsJsonAsync("/api/v1/telemetry/batches", batch);

        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
        Assert.NotNull(third.Headers.RetryAfter?.Delta);
        Assert.True(third.Headers.RetryAfter!.Delta!.Value.TotalSeconds >= 1);
        Assert.Equal("application/problem+json", third.Content.Headers.ContentType?.MediaType);
    }
}
