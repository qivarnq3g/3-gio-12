using System.Text.Json.Nodes;

namespace ThreeTwelve.Api.Tests;

public static class TelemetryPayloads
{
    private static int _sequence;

    public static JsonObject Batch(Guid installationId, params JsonObject[] events) => new()
    {
        ["installationId"] = installationId.ToString(),
        ["gameVersion"] = "0.3.0",
        ["platform"] = "windows",
        ["events"] = new JsonArray(events.Select(e => (JsonNode)e).ToArray()),
    };

    public static JsonObject SessionStarted(DateTimeOffset? occurredAt = null) =>
        Event("session_started", occurredAt);

    public static JsonObject SessionEnded(string reason, int durationSeconds, int? night = null, string? areaId = null)
    {
        var e = Event("session_ended");
        e["reason"] = reason;
        e["durationSeconds"] = durationSeconds;
        if (night is not null) e["night"] = night;
        if (areaId is not null) e["areaId"] = areaId;
        return e;
    }

    public static JsonObject NightStarted(int night)
    {
        var e = Event("night_started");
        e["night"] = night;
        e["isReplay"] = false;
        return e;
    }

    public static JsonObject NightCompleted(int night, int durationSeconds, bool isReplay = false)
    {
        var e = Event("night_completed");
        e["night"] = night;
        e["isReplay"] = isReplay;
        e["durationSeconds"] = durationSeconds;
        return e;
    }

    public static JsonObject PlayerCaught(int night, string areaId, string checkpointId)
    {
        var e = Event("player_caught");
        e["night"] = night;
        e["areaId"] = areaId;
        e["checkpointId"] = checkpointId;
        return e;
    }

    public static JsonObject ChoiceMade(int night, string choiceId)
    {
        var e = Event("choice_made");
        e["night"] = night;
        e["choiceId"] = choiceId;
        return e;
    }

    public static JsonObject EndingReached(int night, string endingId)
    {
        var e = Event("ending_reached");
        e["night"] = night;
        e["endingId"] = endingId;
        return e;
    }

    private static JsonObject Event(string type, DateTimeOffset? occurredAt = null) => new()
    {
        ["eventId"] = Guid.NewGuid().ToString(),
        ["sessionId"] = Guid.NewGuid().ToString(),
        ["sequence"] = Interlocked.Increment(ref _sequence),
        ["occurredAt"] = (occurredAt ?? DateTimeOffset.UtcNow.AddMinutes(-5)).ToString("yyyy-MM-ddTHH:mm:ssZ"),
        ["type"] = type,
    };
}
