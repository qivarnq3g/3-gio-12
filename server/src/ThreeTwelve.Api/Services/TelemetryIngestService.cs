using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ThreeTwelve.Api.Contracts;
using ThreeTwelve.Api.Data;
using ThreeTwelve.Api.Domain;

namespace ThreeTwelve.Api.Services;

public interface ITelemetryIngestService
{
    Task<TelemetryBatchResponse> IngestAsync(Guid installationId, string gameVersion, IReadOnlyList<JsonElement> events, CancellationToken ct);
    Task<int> DeleteInstallationAsync(Guid installationId, CancellationToken ct);
}

public sealed class TelemetryIngestService(AppDbContext db, TimeProvider clock) : ITelemetryIngestService
{
    public async Task<TelemetryBatchResponse> IngestAsync(Guid installationId, string gameVersion, IReadOnlyList<JsonElement> events, CancellationToken ct)
    {
        var receivedAt = clock.GetUtcNow();
        var rejected = new List<RejectedEvent>();
        var valid = new List<TelemetryEvent>();
        for (var i = 0; i < events.Count; i++)
        {
            var check = TelemetryEventValidator.Check(events[i], gameVersion, installationId, receivedAt);
            if (check.Event is null)
                rejected.Add(new RejectedEvent(i, check.EventId, check.RejectReason!));
            else
                valid.Add(check.Event);
        }

        var accepted = 0;
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        foreach (var e in valid)
        {
            accepted += await db.Database.ExecuteSqlAsync($"""
                insert into telemetry_events
                    (event_id, installation_id, session_id, sequence, type, occurred_at, received_at, game_version,
                     night, area_id, checkpoint_id, choice_id, ending_id, end_reason, duration_seconds, is_replay,
                     position_x, position_y, position_z)
                values
                    ({e.EventId}, {e.InstallationId}, {e.SessionId}, {e.Sequence}, {e.Type}, {e.OccurredAt}, {e.ReceivedAt}, {e.GameVersion},
                     {e.Night}, {e.AreaId}, {e.CheckpointId}, {e.ChoiceId}, {e.EndingId}, {e.EndReason}, {e.DurationSeconds}, {e.IsReplay},
                     {e.PositionX}, {e.PositionY}, {e.PositionZ})
                on conflict (event_id) do nothing
                """, ct);
        }
        await transaction.CommitAsync(ct);

        return new TelemetryBatchResponse(accepted, valid.Count - accepted, rejected);
    }

    public Task<int> DeleteInstallationAsync(Guid installationId, CancellationToken ct) =>
        db.TelemetryEvents.Where(e => e.InstallationId == installationId).ExecuteDeleteAsync(ct);
}
