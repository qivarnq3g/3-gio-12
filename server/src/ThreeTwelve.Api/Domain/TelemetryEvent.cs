namespace ThreeTwelve.Api.Domain;

public sealed class TelemetryEvent
{
    public Guid EventId { get; set; }
    public Guid InstallationId { get; set; }
    public Guid SessionId { get; set; }
    public int Sequence { get; set; }
    public required string Type { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public required string GameVersion { get; set; }
    public short? Night { get; set; }
    public string? AreaId { get; set; }
    public string? CheckpointId { get; set; }
    public string? ChoiceId { get; set; }
    public string? EndingId { get; set; }
    public string? EndReason { get; set; }
    public int? DurationSeconds { get; set; }
    public bool? IsReplay { get; set; }
    public double? PositionX { get; set; }
    public double? PositionY { get; set; }
    public double? PositionZ { get; set; }
}
