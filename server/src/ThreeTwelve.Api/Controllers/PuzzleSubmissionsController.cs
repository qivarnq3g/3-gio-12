using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ThreeTwelve.Api.Contracts;
using ThreeTwelve.Api.Domain;
using ThreeTwelve.Api.RateLimiting;
using ThreeTwelve.Api.Services;

namespace ThreeTwelve.Api.Controllers;

[ApiController]
[Route("api/v1/puzzles")]
[EnableRateLimiting(RateLimitPolicies.PuzzleSubmit)]
public sealed partial class PuzzleSubmissionsController(IPuzzleSubmissionService submissions) : ControllerBase
{
    private const int MaxGameVersionLength = 16;

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{0,63}$")]
    private static partial Regex PuzzleIdPattern();

    [GeneratedRegex("^[0-9]+\\.[0-9]+\\.[0-9]+$")]
    private static partial Regex GameVersionPattern();

    [GeneratedRegex("^[0-9a-f]{64}$")]
    private static partial Regex Sha256Pattern();

    [HttpPost("{puzzleId}/submissions")]
    public async Task<ActionResult<PuzzleSubmissionResponse>> Post(string puzzleId, PuzzleSubmissionRequest submission, CancellationToken ct)
    {
        if (!PuzzleIdPattern().IsMatch(puzzleId))
            ModelState.AddModelError("puzzleId", "Chữ thường, số và gạch nối, tối đa 64 ký tự.");
        if (submission.InstallationId is null || submission.InstallationId == Guid.Empty)
            ModelState.AddModelError("installationId", "Bắt buộc, UUID.");
        if (submission.GameVersion is null || submission.GameVersion.Length > MaxGameVersionLength || !GameVersionPattern().IsMatch(submission.GameVersion))
            ModelState.AddModelError("gameVersion", $"Bắt buộc, dạng 0.3.0, tối đa {MaxGameVersionLength} ký tự.");
        if (submission.Sha256 is null || !Sha256Pattern().IsMatch(submission.Sha256))
            ModelState.AddModelError("sha256", "Bắt buộc, 64 ký tự hex chữ thường.");
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var outcome = await submissions.SubmitAsync(puzzleId, submission.InstallationId!.Value, submission.GameVersion!, submission.Sha256!, ct);

        switch (outcome.Kind)
        {
            case SubmissionOutcomeKind.PuzzleNotFound:
                return Problem(statusCode: StatusCodes.Status404NotFound, title: "Không có câu đố này",
                    detail: "Câu đố không tồn tại hoặc đang tắt.");
            case SubmissionOutcomeKind.Blocked:
                var retryAfter = Math.Max(1, (int)Math.Ceiling(outcome.RetryAfter!.Value.TotalSeconds));
                Response.Headers.RetryAfter = retryAfter.ToString(CultureInfo.InvariantCulture);
                return Problem(statusCode: StatusCodes.Status429TooManyRequests, title: "Hết lượt gửi",
                    detail: $"Thử lại sau {retryAfter} giây.");
            default:
                return new PuzzleSubmissionResponse(
                    outcome.Kind == SubmissionOutcomeKind.Accepted ? SubmissionResult.Accepted : SubmissionResult.Rejected,
                    outcome.UnlockId,
                    outcome.AttemptsRemaining,
                    WholeSecondUtc(outcome.WindowResetsAt));
        }
    }

    private static DateTime WholeSecondUtc(DateTimeOffset value)
    {
        var ticks = value.UtcTicks;
        var remainder = ticks % TimeSpan.TicksPerSecond;
        return new DateTime(remainder == 0 ? ticks : ticks - remainder + TimeSpan.TicksPerSecond, DateTimeKind.Utc);
    }
}
