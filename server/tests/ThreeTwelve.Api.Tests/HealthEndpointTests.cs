using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ThreeTwelve.Api.Tests;

public sealed class HealthEndpointTests(PostgresApiFactory factory) : IClassFixture<PostgresApiFactory>
{
    [Fact]
    public async Task Returns_ok_when_database_is_reachable()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ok", body.GetProperty("status").GetString());
    }
}

public sealed class HealthEndpointWithoutDatabaseTests(UnreachableDatabaseApiFactory factory) : IClassFixture<UnreachableDatabaseApiFactory>
{
    [Fact]
    public async Task Returns_degraded_with_503_when_database_is_unreachable()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("degraded", body.GetProperty("status").GetString());
    }
}
