using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Time.Testing;
using ThreeTwelve.Api.Contracts;
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

        async Task IngestAsync(Guid installation, params JsonObject[] events)
        {
            using var response = await client.PostAsJsonAsync(BatchUrl, TelemetryPayloads.Batch(installation, events));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<TelemetryBatchResponse>();
            Assert.NotNull(result);
            Assert.Equal(events.Length, result.Accepted);
            Assert.Empty(result.Rejected);
        }

        var occurredAt = factory.Clock.GetUtcNow().AddMinutes(-20);
        await IngestAsync(Guid.NewGuid(), TelemetryPayloads.NightStarted(2, occurredAt));
        factory.Clock.Advance(TimeSpan.FromSeconds(1));
        var from = factory.Clock.GetUtcNow();

        var installationId = Guid.NewGuid();
        var replayStart = TelemetryPayloads.NightStarted(3, occurredAt);
        replayStart["isReplay"] = true;
        await IngestAsync(installationId,
            TelemetryPayloads.SessionStarted(occurredAt),
            TelemetryPayloads.NightStarted(1, occurredAt),
            TelemetryPayloads.NightStarted(1, occurredAt),
            TelemetryPayloads.PlayerCaught(1, "n1.parking", "n1.alley_start", occurredAt),
            TelemetryPayloads.ChoiceMade(1, "1a", occurredAt),
            TelemetryPayloads.NightCompleted(1, 300, occurredAt: occurredAt),
            TelemetryPayloads.NightCompleted(1, 500, occurredAt: occurredAt),
            TelemetryPayloads.NightCompleted(1, 30, isReplay: true, occurredAt: occurredAt),
            TelemetryPayloads.NightCompleted(2, 600, occurredAt: occurredAt),
            replayStart,
            TelemetryPayloads.NightCompleted(3, 25, isReplay: true, occurredAt: occurredAt),
            TelemetryPayloads.EndingReached(1, "bystander", occurredAt),
            TelemetryPayloads.SessionEnded("quit_to_menu", 450, 1, "n1.parking", occurredAt));

        factory.Clock.Advance(TimeSpan.FromSeconds(1));
        var to = factory.Clock.GetUtcNow();
        var atEnd = TelemetryPayloads.SessionStarted(occurredAt);
        await IngestAsync(Guid.NewGuid(), atEnd);
        var range = $"from={Uri.EscapeDataString(from.ToString("O"))}&to={Uri.EscapeDataString(to.ToString("O"))}";

        using var response = await admin.GetAsync($"{DashboardUrl}?{range}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<DashboardSummary>();
        Assert.NotNull(summary);
        Assert.Equal(1, summary.Players);
        Assert.Equal(1, summary.Sessions);
        Assert.Equal(new PlaytimeStats(450, 450), summary.Playtime);
        Assert.Collection(summary.Nights,
            n => Assert.Equal(new NightStats(1, 1, 1, 400), n),
            n => Assert.Equal(new NightStats(2, 0, 1, 600), n));
        Assert.Equal(new LocationCount(1, "n1.parking", 1), Assert.Single(summary.CaughtLocations));
        Assert.Equal(new LocationCount(1, "n1.parking", 1), Assert.Single(summary.QuitLocations));
        Assert.Equal(new EndingStats("bystander", 1), Assert.Single(summary.Endings));
        Assert.Equal(new ChoiceStats(1, "1a", 1), Assert.Single(summary.Choices));

        var csv = await admin.GetStringAsync($"{ExportsUrl}/summary?{range}");
        Assert.Equal(new[]
        {
            "metric,night,key,value", "players,,,1", "sessions,,,1",
            "playtime_total_seconds,,,450", "playtime_average_seconds,,,450",
            "night_started,1,,1", "night_completed,1,,1", "night_duration_seconds,1,,400",
            "night_started,2,,0", "night_completed,2,,1", "night_duration_seconds,2,,600",
            "caught,1,n1.parking,1", "quit,1,n1.parking,1", "ending,,bystander,1", "choice,1,1a,1"
        }, csv.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));

        var rawCsv = await admin.GetStringAsync($"{ExportsUrl}/events?{range}");
        Assert.DoesNotContain(atEnd["eventId"]!.GetValue<string>(), rawCsv);
        Assert.Equal(14, rawCsv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);
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
