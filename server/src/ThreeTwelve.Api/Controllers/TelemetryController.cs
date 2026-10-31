using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ThreeTwelve.Api.Contracts;
using ThreeTwelve.Api.RateLimiting;
using ThreeTwelve.Api.Services;

namespace ThreeTwelve.Api.Controllers;

[ApiController]
[Route("api/v1/telemetry")]
[EnableRateLimiting(RateLimitPolicies.Telemetry)]
public sealed partial class TelemetryController(ITelemetryIngestService telemetry) : ControllerBase
{
    public const int MaxBatchBytes = 256 * 1024;
    private const int MaxEvents = 100;

    [GeneratedRegex("^[0-9]+\\.[0-9]+\\.[0-9]+$")]
    private static partial Regex GameVersionPattern();

    [HttpPost("batches")]
    [RequestSizeLimit(MaxBatchBytes)]
    [RejectLargeBody(MaxBatchBytes)]
    public async Task<ActionResult<TelemetryBatchResponse>> PostBatch(TelemetryBatchRequest batch, CancellationToken ct)
    {
        if (batch.InstallationId is null || batch.InstallationId == Guid.Empty)
            ModelState.AddModelError("installationId", "Bắt buộc, UUID.");
        if (batch.GameVersion is null || !GameVersionPattern().IsMatch(batch.GameVersion))
            ModelState.AddModelError("gameVersion", "Bắt buộc, dạng 0.3.0.");
        if (batch.Platform != "windows")
            ModelState.AddModelError("platform", "Chỉ nhận windows.");
        if (batch.Events is null || batch.Events.Count is 0 or > MaxEvents)
            ModelState.AddModelError("events", $"Từ 1 tới {MaxEvents} sự kiện.");
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        return await telemetry.IngestAsync(batch.InstallationId!.Value, batch.GameVersion!, batch.Events!, ct);
    }

    [HttpDelete("installations/{installationId}")]
    public async Task<IActionResult> DeleteInstallation(Guid installationId, CancellationToken ct)
    {
        await telemetry.DeleteInstallationAsync(installationId, ct);
        return NoContent();
    }
}
