namespace ThreeTwelve.Api.Domain;

public sealed class PuzzleSubmission
{
    public long Id { get; set; }
    public required string PuzzleId { get; set; }
    public Guid InstallationId { get; set; }
    public required string Sha256 { get; set; }
    public required string Result { get; set; }
    public required string GameVersion { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
}

public static class SubmissionResult
{
    public const string Accepted = "accepted";
    public const string Rejected = "rejected";
    public const string Blocked = "blocked";
}
