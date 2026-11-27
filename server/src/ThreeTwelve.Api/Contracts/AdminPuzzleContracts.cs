using System.Text.Json.Serialization;
using ThreeTwelve.Api.Domain;

namespace ThreeTwelve.Api.Contracts;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UpdatePuzzleRequest(bool? IsEnabled, int? MaxFailedAttempts, int? AttemptWindowMinutes);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AddPuzzleSolutionRequest(string? Sha256, string? Note);

public sealed record PuzzleSolutionResponse(int Id, string Sha256, string Note, DateTime CreatedAt)
{
    public static PuzzleSolutionResponse From(PuzzleSolution s) => new(s.Id, s.Sha256, s.Note, s.CreatedAt.UtcDateTime);
}

public sealed record PuzzleConfigResponse(
    string PuzzleId,
    string Title,
    string UnlockId,
    bool IsEnabled,
    int MaxFailedAttempts,
    int AttemptWindowMinutes,
    List<PuzzleSolutionResponse> Solutions,
    DateTime UpdatedAt)
{
    public static PuzzleConfigResponse From(Puzzle p) => new(
        p.Id,
        p.Title,
        p.UnlockId,
        p.IsEnabled,
        p.MaxFailedAttempts,
        p.AttemptWindowMinutes,
        p.Solutions.OrderBy(s => s.Id).Select(PuzzleSolutionResponse.From).ToList(),
        p.UpdatedAt.UtcDateTime);
}

public sealed record PuzzleSubmissionRecord(
    long Id,
    Guid InstallationId,
    string Sha256,
    string Result,
    string GameVersion,
    DateTime SubmittedAt)
{
    public static PuzzleSubmissionRecord From(PuzzleSubmission s) =>
        new(s.Id, s.InstallationId, s.Sha256, s.Result, s.GameVersion, s.SubmittedAt.UtcDateTime);
}

public sealed record PuzzleSubmissionPage(List<PuzzleSubmissionRecord> Items, int Page, int PageSize, int Total);
