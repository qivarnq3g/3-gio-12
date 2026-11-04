using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using ThreeTwelve.Api.Domain;
using ThreeTwelve.Api.Services;

namespace ThreeTwelve.Api.Tests;

public sealed class AdminDashboardTests(AdminApiFactory factory) : IClassFixture<AdminApiFactory>
{
    private const string DashboardUrl = "/api/admin/dashboard";
    private const string ExportsUrl = "/api/admin/exports";
    private const string BatchUrl = "/api/v1/telemetry/batches";

    [Fact]
    public void DateRange_defaults_to_clock_utc_and_30_days_before()
    {
        var fixedTime = new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.FromHours(7));
        var clock = new FakeTimeProvider(fixedTime);

        var range = DateRange.Resolve(null, null, clock);

        Assert.Equal(fixedTime.ToUniversalTime(), range.To);
        Assert.Equal(fixedTime.ToUniversalTime().AddDays(-30), range.From);
    }

    [Fact]
    public void DateRange_throws_when_from_is_not_before_to()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var now = clock.GetUtcNow();

        Assert.Throws<BadRequestException>(() =>
            DateRange.Resolve(now, now, clock));
        Assert.Throws<BadRequestException>(() =>
            DateRange.Resolve(now.AddDays(1), now, clock));
    }

    [Fact]
    public void DateRange_throws_when_range_exceeds_366_days()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var to = clock.GetUtcNow();
        var from = to.AddDays(-367);

        Assert.Throws<BadRequestException>(() =>
            DateRange.Resolve(from, to, clock));
    }

    [Fact]
    public async Task Dashboard_requires_authentication()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(DashboardUrl);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_returns_400_when_date_range_is_invalid()
    {
        using var admin = await factory.LoginAsync();

        var response = await admin.GetAsync($"{DashboardUrl}?from=2026-12-01T00:00:00Z&to=2026-11-01T00:00:00Z");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Dashboard_returns_metrics_matching_ingested_events()
    {
        using var client = factory.CreateClient();
        using var admin = await factory.LoginAsync();

        var installationId = Guid.NewGuid();
        var batch = TelemetryPayloads.Batch(installationId,
            TelemetryPayloads.SessionStarted(),
            TelemetryPayloads.NightStarted(1),
            TelemetryPayloads.PlayerCaught(1, "n1.parking", "n1.alley_start"),
            TelemetryPayloads.ChoiceMade(1, "1a"),
            TelemetryPayloads.NightCompleted(1, durationSeconds: 300, isReplay: false),
            TelemetryPayloads.EndingReached(1, "bystander"),
            TelemetryPayloads.SessionEnded(reason: "quit_menu", durationSeconds: 450, night: 1, areaId: "n1.parking")
        );

        var batchRes = await client.PostAsJsonAsync(BatchUrl, batch);
        Assert.Equal(HttpStatusCode.OK, batchRes.StatusCode);

        var response = await admin.GetAsync(DashboardUrl);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var summary = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(summary.GetProperty("players").GetInt32() >= 1);
        Assert.True(summary.GetProperty("sessions").GetInt32() >= 1);
        Assert.True(summary.GetProperty("playtime").GetProperty("totalSeconds").GetInt32() >= 450);

        var nights = summary.GetProperty("nights").EnumerateArray().ToList();
        var n1 = nights.FirstOrDefault(n => n.GetProperty("night").GetInt32() == 1);
        Assert.True(n1.ValueKind != JsonValueKind.Undefined);
        Assert.True(n1.GetProperty("playersStarted").GetInt32() >= 1);
        Assert.True(n1.GetProperty("playersCompleted").GetInt32() >= 1);

        var caught = summary.GetProperty("caughtLocations").EnumerateArray().ToList();
        Assert.Contains(caught, c => c.GetProperty("areaId").GetString() == "n1.parking");

        var quits = summary.GetProperty("quitLocations").EnumerateArray().ToList();
        Assert.Contains(quits, q => q.GetProperty("areaId").GetString() == "n1.parking");

        var endings = summary.GetProperty("endings").EnumerateArray().ToList();
        Assert.Contains(endings, e => e.GetProperty("endingId").GetString() == "bystander");

        var choices = summary.GetProperty("choices").EnumerateArray().ToList();
        Assert.Contains(choices, c => c.GetProperty("choiceId").GetString() == "1a");
    }

    [Fact]
    public async Task Export_summary_returns_csv_with_utf8_bom()
    {
        using var admin = await factory.LoginAsync();

        var response = await admin.GetAsync($"{ExportsUrl}/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("attachment; filename=\"3gio12-summary-", response.Content.Headers.GetValues("Content-Disposition").First());

        var bytes = await response.Content.ReadAsByteArrayAsync();
        // UTF-8 BOM is 0xEF, 0xBB, 0xBF
        Assert.True(bytes.Length >= 3);
        Assert.Equal(0xEF, bytes[0]);
        Assert.Equal(0xBB, bytes[1]);
        Assert.Equal(0xBF, bytes[2]);

        var text = System.Text.Encoding.UTF8.GetString(bytes);
        Assert.StartsWith("\uFEFFmetric,night,key,value", text);
        Assert.Contains("players,,,", text);
        Assert.Contains("sessions,,,", text);
    }

    [Fact]
    public async Task Export_events_returns_csv_with_utf8_bom()
    {
        using var admin = await factory.LoginAsync();

        var response = await admin.GetAsync($"{ExportsUrl}/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("attachment; filename=\"3gio12-events-", response.Content.Headers.GetValues("Content-Disposition").First());

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length >= 3);
        Assert.Equal(0xEF, bytes[0]);
        Assert.Equal(0xBB, bytes[1]);
        Assert.Equal(0xBF, bytes[2]);

        var text = System.Text.Encoding.UTF8.GetString(bytes);
        Assert.StartsWith("\uFEFFevent_id,installation_id,session_id,sequence,type", text);
    }

    [Fact]
    public async Task Export_unknown_dataset_answers_400()
    {
        using var admin = await factory.LoginAsync();

        var response = await admin.GetAsync($"{ExportsUrl}/unknown");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
