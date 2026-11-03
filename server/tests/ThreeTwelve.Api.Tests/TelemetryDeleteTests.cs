using System.Net;
using System.Net.Http.Json;
using Npgsql;

namespace ThreeTwelve.Api.Tests;

public sealed class TelemetryDeleteTests(PostgresApiFactory factory) : IClassFixture<PostgresApiFactory>
{
    [Fact]
    public async Task Deletes_every_event_of_one_installation_only()
    {
        using var client = factory.CreateClient();
        var leaving = Guid.NewGuid();
        var staying = Guid.NewGuid();
        await client.PostAsJsonAsync("/api/v1/telemetry/batches",
            TelemetryPayloads.Batch(leaving, TelemetryPayloads.SessionStarted(), TelemetryPayloads.NightStarted(1)));
        await client.PostAsJsonAsync("/api/v1/telemetry/batches",
            TelemetryPayloads.Batch(staying, TelemetryPayloads.SessionStarted()));

        var response = await client.DeleteAsync($"/api/v1/telemetry/installations/{leaving}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(0, await CountEventsAsync(leaving));
        Assert.Equal(1, await CountEventsAsync(staying));
    }

    [Fact]
    public async Task Deleting_an_unknown_installation_still_returns_204()
    {
        using var client = factory.CreateClient();

        var response = await client.DeleteAsync($"/api/v1/telemetry/installations/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Rejects_an_installation_id_that_is_not_a_uuid_with_400()
    {
        using var client = factory.CreateClient();

        var response = await client.DeleteAsync("/api/v1/telemetry/installations/not-a-uuid");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<long> CountEventsAsync(Guid installationId)
    {
        await using var connection = new NpgsqlConnection(factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "select count(*) from telemetry_events where installation_id = @id", connection);
        command.Parameters.AddWithValue("id", installationId);
        return (long)(await command.ExecuteScalarAsync())!;
    }
}
