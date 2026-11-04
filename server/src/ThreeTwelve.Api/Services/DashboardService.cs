using Microsoft.EntityFrameworkCore;
using ThreeTwelve.Api.Contracts;
using ThreeTwelve.Api.Data;
using ThreeTwelve.Api.Domain;

namespace ThreeTwelve.Api.Services;

public sealed class DashboardService(AppDbContext db) : IDashboardService
{
    public async Task<DashboardSummary> GetSummaryAsync(DateRange range, CancellationToken ct = default)
    {
        var players = await db.TelemetryEvents
            .Where(e => e.ReceivedAt >= range.From && e.ReceivedAt < range.To)
            .Select(e => e.InstallationId)
            .Distinct()
            .CountAsync(ct);

        var sessions = await db.TelemetryEvents
            .Where(e => e.ReceivedAt >= range.From && e.ReceivedAt < range.To && e.Type == "session_started")
            .CountAsync(ct);

        var endedStats = await db.TelemetryEvents
            .Where(e => e.ReceivedAt >= range.From && e.ReceivedAt < range.To && e.Type == "session_ended" && e.DurationSeconds != null)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Sum(e => (long)e.DurationSeconds!.Value),
                Count = g.Count(),
                Average = g.Average(e => (double?)e.DurationSeconds)
            })
            .FirstOrDefaultAsync(ct);

        var totalSeconds = endedStats is not null ? (int)Math.Min(endedStats.Total, int.MaxValue) : 0;
        var avgSessionSeconds = endedStats?.Count > 0 ? endedStats.Average : null;

        var startedNights = await db.TelemetryEvents
            .Where(e => e.ReceivedAt >= range.From && e.ReceivedAt < range.To && e.Type == "night_started" && e.Night != null && e.IsReplay == false)
            .GroupBy(e => e.Night!.Value)
            .Select(g => new
            {
                Night = g.Key,
                PlayersStarted = g.Select(e => e.InstallationId).Distinct().Count()
            })
            .ToListAsync(ct);

        var completedNights = await db.TelemetryEvents
            .Where(e => e.ReceivedAt >= range.From && e.ReceivedAt < range.To && e.Type == "night_completed" && e.Night != null && e.IsReplay == false)
            .GroupBy(e => e.Night!.Value)
            .Select(g => new
            {
                Night = g.Key,
                PlayersCompleted = g.Select(e => e.InstallationId).Distinct().Count(),
                AverageDurationSeconds = g.Average(e => (double?)e.DurationSeconds)
            })
            .ToListAsync(ct);

        var completedLookup = completedNights.ToDictionary(c => c.Night);
        var nights = startedNights
            .OrderBy(s => s.Night)
            .Select(s =>
            {
                completedLookup.TryGetValue(s.Night, out var comp);
                return new NightStats(
                    Night: s.Night,
                    PlayersStarted: s.PlayersStarted,
                    PlayersCompleted: comp?.PlayersCompleted ?? 0,
                    AverageDurationSeconds: comp?.AverageDurationSeconds
                );
            })
            .ToList();

        var caughtLocations = await db.TelemetryEvents
            .Where(e => e.ReceivedAt >= range.From && e.ReceivedAt < range.To && e.Type == "player_caught" && e.Night != null && e.AreaId != null)
            .GroupBy(e => new { Night = e.Night!.Value, AreaId = e.AreaId! })
            .Select(g => new
            {
                g.Key.Night,
                g.Key.AreaId,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Night)
            .ThenBy(x => x.AreaId)
            .ToListAsync(ct);

        var quitLocations = await db.TelemetryEvents
            .Where(e => e.ReceivedAt >= range.From && e.ReceivedAt < range.To && e.Type == "session_ended" && e.Night != null && e.AreaId != null)
            .GroupBy(e => new { Night = e.Night!.Value, AreaId = e.AreaId! })
            .Select(g => new
            {
                g.Key.Night,
                g.Key.AreaId,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Night)
            .ThenBy(x => x.AreaId)
            .ToListAsync(ct);

        var endings = await db.TelemetryEvents
            .Where(e => e.ReceivedAt >= range.From && e.ReceivedAt < range.To && e.Type == "ending_reached" && e.EndingId != null)
            .GroupBy(e => e.EndingId!)
            .Select(g => new
            {
                EndingId = g.Key,
                Players = g.Select(e => e.InstallationId).Distinct().Count()
            })
            .OrderByDescending(x => x.Players)
            .ThenBy(x => x.EndingId)
            .ToListAsync(ct);

        var choices = await db.TelemetryEvents
            .Where(e => e.ReceivedAt >= range.From && e.ReceivedAt < range.To && e.Type == "choice_made" && e.Night != null && e.ChoiceId != null)
            .GroupBy(e => new { Night = e.Night!.Value, ChoiceId = e.ChoiceId! })
            .Select(g => new
            {
                g.Key.Night,
                g.Key.ChoiceId,
                Players = g.Select(e => e.InstallationId).Distinct().Count()
            })
            .OrderBy(x => x.Night)
            .ThenBy(x => x.ChoiceId)
            .ToListAsync(ct);

        return new DashboardSummary(
            From: range.From,
            To: range.To,
            Players: players,
            Sessions: sessions,
            Playtime: new PlaytimeStats(totalSeconds, avgSessionSeconds),
            Nights: nights,
            CaughtLocations: caughtLocations.Select(x => new LocationCount(x.Night, x.AreaId, x.Count)).ToList(),
            QuitLocations: quitLocations.Select(x => new LocationCount(x.Night, x.AreaId, x.Count)).ToList(),
            Endings: endings.Select(x => new EndingStats(x.EndingId, x.Players)).ToList(),
            Choices: choices.Select(x => new ChoiceStats(x.Night, x.ChoiceId, x.Players)).ToList()
        );
    }
}
