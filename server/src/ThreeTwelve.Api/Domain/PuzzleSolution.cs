namespace ThreeTwelve.Api.Domain;

public sealed class PuzzleSolution
{
    public int Id { get; set; }
    public required string PuzzleId { get; set; }
    public required string Sha256 { get; set; }
    public string Note { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedById { get; set; }
}
