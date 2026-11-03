using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Npgsql;

namespace ThreeTwelve.Api.Tests;

public sealed class TelemetryBatchTests(PostgresApiFactory factory) : IClassFixture<PostgresApiFactory>
{
    private const string BatchUrl = "/api/v1/telemetry/batches";

    [Fact]
    public async Task Stores_every_valid_event_and_reports_them_accepted()
    {
        using var client = factory.CreateClient();
        var installationId = Guid.NewGuid();
        var batch = TelemetryPayloads.Batch(installationId,
            TelemetryPayloads.SessionStarted(),
            TelemetryPayloads.NightStarted(1),
            TelemetryPayloads.ChoiceMade(1, "0b"));

        var response = await client.PostAsJsonAsync(BatchUrl, batch);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, body.GetProperty("accepted").GetInt32());
        Assert.Equal(0, body.GetProperty("duplicates").GetInt32());
        Assert.Equal(0, body.GetProperty("rejected").GetArrayLength());
        Assert.Equal(3, await CountEventsAsync(installationId));
    }

    [Fact]
    public async Task Resending_the_same_batch_counts_every_event_as_duplicate()
    {
        using var client = factory.CreateClient();
        var installationId = Guid.NewGuid();
        var batch = TelemetryPayloads.Batch(installationId,
            TelemetryPayloads.SessionStarted(),
            TelemetryPayloads.NightCompleted(1, 1800));

        await client.PostAsJsonAsync(BatchUrl, batch);
        var response = await client.PostAsJsonAsync(BatchUrl, batch);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, body.GetProperty("accepted").GetInt32());
        Assert.Equal(2, body.GetProperty("duplicates").GetInt32());
        Assert.Equal(2, await CountEventsAsync(installationId));
    }

    [Fact]
    public async Task Same_event_twice_in_one_batch_is_stored_once()
    {
        using var client = factory.CreateClient();
        var installationId = Guid.NewGuid();
        var started = TelemetryPayloads.SessionStarted();
        var batch = TelemetryPayloads.Batch(installationId, started, started.DeepClone().AsObject());

        var response = await client.PostAsJsonAsync(BatchUrl, batch);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, body.GetProperty("accepted").GetInt32());
        Assert.Equal(1, body.GetProperty("duplicates").GetInt32());
        Assert.Equal(1, await CountEventsAsync(installationId));
    }

    [Fact]
    public async Task Rejects_bad_events_one_by_one_and_keeps_the_rest()
    {
        using var client = factory.CreateClient();
        var installationId = Guid.NewGuid();
        var unknown = TelemetryPayloads.SessionStarted();
        unknown["type"] = "door_opened";
        var tooOld = TelemetryPayloads.SessionStarted(DateTimeOffset.UtcNow.AddDays(-31));
        var inFuture = TelemetryPayloads.SessionStarted(DateTimeOffset.UtcNow.AddHours(25));
        var missingNight = TelemetryPayloads.NightStarted(1);
        missingNight.Remove("night");
        var wrongType = TelemetryPayloads.PlayerCaught(1, "n1.parking", "n1.alley_start");
        wrongType["night"] = "one";
        var missingReplay = TelemetryPayloads.NightCompleted(1, 1800);
        missingReplay.Remove("isReplay");
        var valid = TelemetryPayloads.EndingReached(1, "bystander");
        var batch = TelemetryPayloads.Batch(installationId, unknown, tooOld, inFuture, missingNight, wrongType, missingReplay, valid);

        var response = await client.PostAsJsonAsync(BatchUrl, batch);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, body.GetProperty("accepted").GetInt32());
        var rejected = body.GetProperty("rejected").EnumerateArray()
            .Select(r => (r.GetProperty("index").GetInt32(), r.GetProperty("reason").GetString()))
            .ToList();
        Assert.Equal(
            [(0, "unknown_type"), (1, "too_old"), (2, "in_future"), (3, "invalid"), (4, "invalid"), (5, "invalid")],
            rejected);
        Assert.Equal(unknown["eventId"]!.GetValue<string>(),
            body.GetProperty("rejected")[0].GetProperty("eventId").GetString());
        Assert.Equal(1, await CountEventsAsync(installationId));
    }

    [Fact]
    public async Task Stores_the_fields_of_each_event_type()
    {
        using var client = factory.CreateClient();
        var installationId = Guid.NewGuid();
        var caught = TelemetryPayloads.PlayerCaught(1, "n1.parking", "n1.alley_start");
        caught["position"] = new JsonObject { ["x"] = 1.5, ["y"] = 0, ["z"] = -2.25 };
        var ended = TelemetryPayloads.SessionEnded("quit_app", 640, 1, "n1.alley");
        var replayed = TelemetryPayloads.NightCompleted(1, 1500, isReplay: true);
        var batch = TelemetryPayloads.Batch(installationId, caught, ended, replayed);

        await client.PostAsJsonAsync(BatchUrl, batch);

        await using var connection = new NpgsqlConnection(factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "select type, night, area_id, checkpoint_id, position_x, position_z, end_reason, duration_seconds, game_version, is_replay " +
            "from telemetry_events where installation_id = @id order by sequence", connection);
        command.Parameters.AddWithValue("id", installationId);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal("player_caught", reader.GetString(0));
        Assert.Equal(1, reader.GetInt16(1));
        Assert.Equal("n1.parking", reader.GetString(2));
        Assert.Equal("n1.alley_start", reader.GetString(3));
        Assert.Equal(1.5, reader.GetDouble(4));
        Assert.Equal(-2.25, reader.GetDouble(5));
        Assert.Equal("0.3.0", reader.GetString(8));
        Assert.True(await reader.ReadAsync());
        Assert.Equal("session_ended", reader.GetString(0));
        Assert.Equal("quit_app", reader.GetString(6));
        Assert.Equal(640, reader.GetInt32(7));
        Assert.True(await reader.ReadAsync());
        Assert.Equal("night_completed", reader.GetString(0));
        Assert.Equal(1500, reader.GetInt32(7));
        Assert.True(reader.GetBoolean(9));
    }

    [Theory]
    [InlineData("missingInstallation")]
    [InlineData("emptyEvents")]
    [InlineData("tooManyEvents")]
    [InlineData("badVersion")]
    [InlineData("badPlatform")]
    [InlineData("versionTooLong")]
    [InlineData("unknownField")]
    public async Task Rejects_a_broken_envelope_with_400(string variant)
    {
        using var client = factory.CreateClient();
        var installationId = Guid.NewGuid();
        var batch = TelemetryPayloads.Batch(installationId, TelemetryPayloads.SessionStarted());
        switch (variant)
        {
            case "missingInstallation": batch.Remove("installationId"); break;
            case "emptyEvents": batch["events"] = new JsonArray(); break;
            case "tooManyEvents":
                batch["events"] = new JsonArray(Enumerable.Range(0, 101)
                    .Select(_ => (JsonNode)TelemetryPayloads.SessionStarted()).ToArray());
                break;
            case "badVersion": batch["gameVersion"] = "v0.3"; break;
            case "badPlatform": batch["platform"] = "linux"; break;
            case "versionTooLong": batch["gameVersion"] = "12345678901234567.0.0"; break;
            case "unknownField": batch["unexpected"] = "x"; break;
        }

        var response = await client.PostAsJsonAsync(BatchUrl, batch);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(0, await CountEventsAsync(installationId));
    }

    [Fact]
    public async Task Accepts_a_game_version_of_exactly_16_characters()
    {
        using var client = factory.CreateClient();
        var installationId = Guid.NewGuid();
        var batch = TelemetryPayloads.Batch(installationId, TelemetryPayloads.SessionStarted());
        batch["gameVersion"] = "123456789012.0.0";

        var response = await client.PostAsJsonAsync(BatchUrl, batch);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, await CountEventsAsync(installationId));
    }

    [Fact]
    public async Task Leaves_event_id_out_of_a_rejection_when_it_is_missing_or_not_a_uuid()
    {
        using var client = factory.CreateClient();
        var malformedId = TelemetryPayloads.SessionStarted();
        malformedId["eventId"] = "not-a-uuid";
        var batch = TelemetryPayloads.Batch(Guid.NewGuid(), new JsonObject(), malformedId);

        var response = await client.PostAsJsonAsync(BatchUrl, batch);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rejected = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("rejected");
        Assert.Equal(2, rejected.GetArrayLength());
        foreach (var entry in rejected.EnumerateArray())
        {
            Assert.False(entry.TryGetProperty("eventId", out _));
            Assert.Equal("invalid", entry.GetProperty("reason").GetString());
        }
    }

    [Fact]
    public async Task Rejects_malformed_json_with_400()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsync(BatchUrl,
            new StringContent("{\"installationId\":", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Rejects_a_body_over_256_KiB_with_413()
    {
        using var client = factory.CreateClient();
        var batch = TelemetryPayloads.Batch(Guid.NewGuid(), TelemetryPayloads.SessionStarted());
        batch["padding"] = new string('x', 256 * 1024);

        var response = await client.PostAsJsonAsync(BatchUrl, batch);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    private async Task<long> CountEventsAsync(Guid installationId)
    {
        _ = factory.Server;
        await using var connection = new NpgsqlConnection(factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "select count(*) from telemetry_events where installation_id = @id", connection);
        command.Parameters.AddWithValue("id", installationId);
        return (long)(await command.ExecuteScalarAsync())!;
    }
}
