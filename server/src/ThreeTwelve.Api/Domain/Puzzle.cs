namespace ThreeTwelve.Api.Domain;

public sealed class Puzzle
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public required string UnlockId { get; set; }
    public bool IsEnabled { get; set; }
    public int MaxFailedAttempts { get; set; } = 5;
    public int AttemptWindowMinutes { get; set; } = 60;
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? UpdatedById { get; set; }
    public List<PuzzleSolution> Solutions { get; set; } = [];
}
