using System.Text.Json;
using System.Text.Json.Serialization;

namespace ThreeTwelve.Api.Contracts;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record TelemetryBatchRequest(
    Guid? InstallationId,
    string? GameVersion,
    string? Platform,
    List<JsonElement>? Events);

public sealed record RejectedEvent(
    int Index,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Guid? EventId,
    string Reason);

public sealed record TelemetryBatchResponse(int Accepted, int Duplicates, IReadOnlyList<RejectedEvent> Rejected);
