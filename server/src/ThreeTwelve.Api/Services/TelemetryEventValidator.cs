using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using ThreeTwelve.Api.Domain;

namespace ThreeTwelve.Api.Services;

public sealed record TelemetryEventCheck(TelemetryEvent? Event, Guid? EventId, string? RejectReason);

public static partial class TelemetryEventValidator
{
    public const string Invalid = "invalid";
    public const string UnknownType = "unknown_type";
    public const string TooOld = "too_old";
    public const string InFuture = "in_future";

    private static readonly HashSet<string> KnownTypes =
    [
        "session_started", "session_ended", "night_started", "night_completed",
        "player_caught", "choice_made", "ending_reached",
    ];

    private static readonly HashSet<string> EndReasons = ["quit_to_menu", "quit_app", "not_closed"];

    private static readonly HashSet<string> EndingIds =
    [
        "bystander", "scapegoat", "freezer", "empty_room", "deadline_passed", "home", "red_thread_sandal",
    ];

    [GeneratedRegex("^[a-z0-9][a-z0-9_.-]{0,63}$")]
    private static partial Regex LocationId();

    [GeneratedRegex("^[0-9]{1,2}[a-z]?$")]
    private static partial Regex ChoiceIdPattern();

    public static TelemetryEventCheck Check(JsonElement json, string gameVersion, Guid installationId, DateTimeOffset receivedAt)
    {
        if (json.ValueKind != JsonValueKind.Object)
            return Reject(null, Invalid);

        var eventId = ReadGuid(json, "eventId");
        var sessionId = ReadGuid(json, "sessionId");
        var sequence = ReadInt(json, "sequence");
        var occurredAt = ReadTimestamp(json, "occurredAt");
        var type = ReadString(json, "type");
        if (eventId is null || sessionId is null || sequence is not >= 0 || occurredAt is null || type is null)
            return Reject(eventId, Invalid);

        if (!KnownTypes.Contains(type))
            return Reject(eventId, UnknownType);
        if (occurredAt < receivedAt.AddDays(-30))
            return Reject(eventId, TooOld);
        if (occurredAt > receivedAt.AddHours(24))
            return Reject(eventId, InFuture);

        var entity = new TelemetryEvent
        {
            EventId = eventId.Value,
            InstallationId = installationId,
            SessionId = sessionId.Value,
            Sequence = sequence.Value,
            Type = type,
            OccurredAt = occurredAt.Value,
            ReceivedAt = receivedAt,
            GameVersion = gameVersion,
        };

        return FillTypeFields(json, entity) ? new TelemetryEventCheck(entity, eventId, null) : Reject(eventId, Invalid);
    }

    private static bool FillTypeFields(JsonElement json, TelemetryEvent e)
    {
        switch (e.Type)
        {
            case "session_started":
                return true;
            case "session_ended":
                e.EndReason = ReadString(json, "reason");
                e.DurationSeconds = ReadInt(json, "durationSeconds");
                if (e.EndReason is null || !EndReasons.Contains(e.EndReason) || e.DurationSeconds is not >= 0)
                    return false;
                return OptionalNight(json, e) && OptionalLocation(json, "areaId", v => e.AreaId = v);
            case "night_started":
                e.IsReplay = ReadBool(json, "isReplay");
                return RequiredNight(json, e) && e.IsReplay is not null;
            case "night_completed":
                e.DurationSeconds = ReadInt(json, "durationSeconds");
                return RequiredNight(json, e) && e.DurationSeconds is >= 0;
            case "player_caught":
                e.AreaId = ReadString(json, "areaId");
                e.CheckpointId = ReadString(json, "checkpointId");
                return RequiredNight(json, e)
                    && e.AreaId is not null && LocationId().IsMatch(e.AreaId)
                    && e.CheckpointId is not null && LocationId().IsMatch(e.CheckpointId)
                    && OptionalPosition(json, e);
            case "choice_made":
                e.ChoiceId = ReadString(json, "choiceId");
                return RequiredNight(json, e) && e.ChoiceId is not null && ChoiceIdPattern().IsMatch(e.ChoiceId);
            case "ending_reached":
                e.EndingId = ReadString(json, "endingId");
                return RequiredNight(json, e) && e.EndingId is not null && EndingIds.Contains(e.EndingId);
            default:
                return false;
        }
    }

    private static bool RequiredNight(JsonElement json, TelemetryEvent e)
    {
        var night = ReadInt(json, "night");
        if (night is not (>= 1 and <= 9))
            return false;
        e.Night = (short)night.Value;
        return true;
    }

    private static bool OptionalNight(JsonElement json, TelemetryEvent e) =>
        !json.TryGetProperty("night", out _) || RequiredNight(json, e);

    private static bool OptionalLocation(JsonElement json, string name, Action<string> assign)
    {
        if (!json.TryGetProperty(name, out _))
            return true;
        var value = ReadString(json, name);
        if (value is null || !LocationId().IsMatch(value))
            return false;
        assign(value);
        return true;
    }

    private static bool OptionalPosition(JsonElement json, TelemetryEvent e)
    {
        if (!json.TryGetProperty("position", out var position))
            return true;
        if (position.ValueKind != JsonValueKind.Object)
            return false;
        e.PositionX = ReadDouble(position, "x");
        e.PositionY = ReadDouble(position, "y");
        e.PositionZ = ReadDouble(position, "z");
        return e.PositionX is not null && e.PositionY is not null && e.PositionZ is not null;
    }

    private static TelemetryEventCheck Reject(Guid? eventId, string reason) => new(null, eventId, reason);

    private static string? ReadString(JsonElement json, string name) =>
        json.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static Guid? ReadGuid(JsonElement json, string name) =>
        Guid.TryParse(ReadString(json, name), out var g) ? g : null;

    private static int? ReadInt(JsonElement json, string name) =>
        json.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i) ? i : null;

    private static double? ReadDouble(JsonElement json, string name) =>
        json.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var d) ? d : null;

    private static bool? ReadBool(JsonElement json, string name) =>
        json.TryGetProperty(name, out var v) && v.ValueKind is JsonValueKind.True or JsonValueKind.False ? v.GetBoolean() : null;

    private static DateTimeOffset? ReadTimestamp(JsonElement json, string name) =>
        DateTimeOffset.TryParse(ReadString(json, name), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var t)
            ? t.ToUniversalTime()
            : null;
}
