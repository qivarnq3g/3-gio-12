namespace ThreeTwelve.Api.Contracts;

public sealed record PlaytimeStats(
    int TotalSeconds,
    double? AverageSessionSeconds);

public sealed record NightStats(
    short Night,
    int PlayersStarted,
    int PlayersCompleted,
    double? AverageDurationSeconds);

public sealed record LocationCount(
    short Night,
    string AreaId,
    int Count);

public sealed record EndingStats(
    string EndingId,
    int Players);

public sealed record ChoiceStats(
    short Night,
    string ChoiceId,
    int Players);

public sealed record DashboardSummary(
    DateTimeOffset From,
    DateTimeOffset To,
    int Players,
    int Sessions,
    PlaytimeStats Playtime,
    IReadOnlyList<NightStats> Nights,
    IReadOnlyList<LocationCount> CaughtLocations,
    IReadOnlyList<LocationCount> QuitLocations,
    IReadOnlyList<EndingStats> Endings,
    IReadOnlyList<ChoiceStats> Choices);
