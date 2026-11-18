using ThreeTwelve.Api.Domain;

namespace ThreeTwelve.Api.Services;

public interface ICsvExportService
{
    Task WriteSummaryAsync(DateRange range, Stream stream, CancellationToken ct = default);
    Task WriteEventsAsync(DateRange range, Stream stream, CancellationToken ct = default);
}
