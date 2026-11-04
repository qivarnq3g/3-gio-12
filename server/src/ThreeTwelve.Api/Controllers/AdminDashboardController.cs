using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThreeTwelve.Api.Contracts;
using ThreeTwelve.Api.Domain;
using ThreeTwelve.Api.Services;

namespace ThreeTwelve.Api.Controllers;

[ApiController]
[Authorize]
public sealed class AdminDashboardController(
    IDashboardService dashboard,
    ICsvExportService exports,
    TimeProvider clock) : ControllerBase
{
    [HttpGet("api/admin/dashboard")]
    public async Task<ActionResult<DashboardSummary>> Get(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken ct)
    {
        var range = DateRange.Resolve(from, to, clock);
        return await dashboard.GetSummaryAsync(range, ct);
    }

    [HttpGet("api/admin/exports/{dataset}")]
    public async Task<IActionResult> Export(
        [FromRoute] string dataset,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken ct)
    {
        if (dataset != "summary" && dataset != "events")
        {
            ModelState.AddModelError("dataset", "Dataset phải là 'summary' hoặc 'events'.");
            return ValidationProblem(ModelState);
        }

        var range = DateRange.Resolve(from, to, clock);
        var fromStr = range.From.ToString("yyyyMMdd");
        var toStr = range.To.ToString("yyyyMMdd");
        var fileName = $"3gio12-{dataset}-{fromStr}-{toStr}.csv";

        Response.ContentType = "text/csv; charset=utf-8";
        Response.Headers.ContentDisposition = $"attachment; filename=\"{fileName}\"";

        if (dataset == "summary")
        {
            await exports.WriteSummaryAsync(range, Response.Body, ct);
        }
        else
        {
            await exports.WriteEventsAsync(range, Response.Body, ct);
        }

        return new EmptyResult();
    }
}
