using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ThreeTwelve.Api.Data;
using ThreeTwelve.Api.Domain;

namespace ThreeTwelve.Api.Services;

public sealed class CsvExportService(IDashboardService dashboard, AppDbContext db) : ICsvExportService
{
    private static readonly UTF8Encoding Utf8WithBom = new(encoderShouldEmitUTF8Identifier: true);

    public async Task WriteSummaryAsync(DateRange range, Stream stream, CancellationToken ct = default)
    {
        var summary = await dashboard.GetSummaryAsync(range, ct);

        await using var writer = new StreamWriter(stream, Utf8WithBom, bufferSize: 4096, leaveOpen: true);

        await writer.WriteLineAsync("metric,night,key,value");
        await writer.WriteLineAsync($"players,,,{summary.Players}");
        await writer.WriteLineAsync($"sessions,,,{summary.Sessions}");
        await writer.WriteLineAsync($"playtime_total_seconds,,,{summary.Playtime.TotalSeconds}");
        await writer.WriteLineAsync($"playtime_average_seconds,,,{FormatDouble(summary.Playtime.AverageSessionSeconds)}");

        foreach (var night in summary.Nights)
        {
            await writer.WriteLineAsync($"night_started,{night.Night},,{night.PlayersStarted}");
            await writer.WriteLineAsync($"night_completed,{night.Night},,{night.PlayersCompleted}");
            await writer.WriteLineAsync($"night_duration_seconds,{night.Night},,{FormatDouble(night.AverageDurationSeconds)}");
        }

        foreach (var caught in summary.CaughtLocations)
        {
            await writer.WriteLineAsync($"caught,{caught.Night},{Escape(caught.AreaId)},{caught.Count}");
        }

        foreach (var quit in summary.QuitLocations)
        {
            await writer.WriteLineAsync($"quit,{quit.Night},{Escape(quit.AreaId)},{quit.Count}");
        }

        foreach (var ending in summary.Endings)
        {
            await writer.WriteLineAsync($"ending,,{Escape(ending.EndingId)},{ending.Players}");
        }

        foreach (var choice in summary.Choices)
        {
            await writer.WriteLineAsync($"choice,{choice.Night},{Escape(choice.ChoiceId)},{choice.Players}");
        }

        await writer.FlushAsync(ct);
    }

    public async Task WriteEventsAsync(DateRange range, Stream stream, CancellationToken ct = default)
    {
        await using var writer = new StreamWriter(stream, Utf8WithBom, bufferSize: 4096, leaveOpen: true);

        await writer.WriteLineAsync("event_id,installation_id,session_id,sequence,type,occurred_at,received_at,game_version,night,area_id,checkpoint_id,choice_id,ending_id,end_reason,duration_seconds,is_replay,position_x,position_y,position_z");

        var events = db.TelemetryEvents
            .AsNoTracking()
            .Where(e => e.ReceivedAt >= range.From && e.ReceivedAt < range.To)
            .OrderBy(e => e.ReceivedAt);

        await foreach (var e in events.AsAsyncEnumerable().WithCancellation(ct))
        {
            var line = string.Join(",",
                e.EventId,
                e.InstallationId,
                e.SessionId,
                e.Sequence,
                Escape(e.Type),
                e.OccurredAt.ToString("O"),
                e.ReceivedAt.ToString("O"),
                Escape(e.GameVersion),
                e.Night?.ToString() ?? "",
                Escape(e.AreaId),
                Escape(e.CheckpointId),
                Escape(e.ChoiceId),
                Escape(e.EndingId),
                Escape(e.EndReason),
                e.DurationSeconds?.ToString() ?? "",
                e.IsReplay is null ? "" : (e.IsReplay.Value ? "true" : "false"),
                FormatDouble(e.PositionX),
                FormatDouble(e.PositionY),
                FormatDouble(e.PositionZ)
            );
            await writer.WriteLineAsync(line);
        }

        await writer.FlushAsync(ct);
    }

    private static string FormatDouble(double? value) =>
        value?.ToString("G", CultureInfo.InvariantCulture) ?? "";

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
