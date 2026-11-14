using System.Text.Json;

namespace ThreeTwelve.Api.Contracts;

public sealed record TelemetryBatchRequest(
    Guid? InstallationId,
    string? GameVersion,
    string? Platform,
    List<JsonElement>? Events);

public sealed record RejectedEvent(int Index, Guid? EventId, string Reason);

public sealed record TelemetryBatchResponse(int Accepted, int Duplicates, IReadOnlyList<RejectedEvent> Rejected);
