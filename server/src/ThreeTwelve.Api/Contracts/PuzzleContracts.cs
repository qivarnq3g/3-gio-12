using System.Text.Json.Serialization;

namespace ThreeTwelve.Api.Contracts;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PuzzleSubmissionRequest(
    Guid? InstallationId,
    string? GameVersion,
    string? Sha256);

public sealed record PuzzleSubmissionResponse(
    string Result,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? UnlockId,
    int AttemptsRemaining,
    DateTime WindowResetsAt);
