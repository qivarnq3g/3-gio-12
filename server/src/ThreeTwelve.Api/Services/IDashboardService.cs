using ThreeTwelve.Api.Contracts;
using ThreeTwelve.Api.Domain;

namespace ThreeTwelve.Api.Services;

public interface IDashboardService
{
    Task<DashboardSummary> GetSummaryAsync(DateRange range, CancellationToken ct = default);
}
