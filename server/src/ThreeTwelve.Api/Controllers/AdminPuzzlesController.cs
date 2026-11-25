using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ThreeTwelve.Api.Contracts;
using ThreeTwelve.Api.Domain;
using ThreeTwelve.Api.Services;

namespace ThreeTwelve.Api.Controllers;

[ApiController]
[Route("api/admin/puzzles")]
[Authorize]
public sealed partial class AdminPuzzlesController(IAdminPuzzleService puzzles, UserManager<AdminUser> users, TimeProvider clock) : ControllerBase
{
    private const int MaxNoteLength = 200;

    [GeneratedRegex("^[0-9a-f]{64}$")]
    private static partial Regex Sha256Pattern();

    [HttpGet]
    public async Task<List<PuzzleConfigResponse>> List(CancellationToken ct) =>
        (await puzzles.ListAsync(ct)).Select(PuzzleConfigResponse.From).ToList();

    [HttpGet("{puzzleId}")]
    public async Task<PuzzleConfigResponse> Get(string puzzleId, CancellationToken ct) =>
        PuzzleConfigResponse.From(await puzzles.GetAsync(puzzleId, ct));

    [HttpPatch("{puzzleId}")]
    public async Task<ActionResult<PuzzleConfigResponse>> Update(string puzzleId, UpdatePuzzleRequest request, CancellationToken ct)
    {
        if (request.IsEnabled is null && request.MaxFailedAttempts is null && request.AttemptWindowMinutes is null)
            ModelState.AddModelError("", "Cần ít nhất một trong isEnabled, maxFailedAttempts, attemptWindowMinutes.");
        if (request.MaxFailedAttempts is < 1 or > 100)
            ModelState.AddModelError("maxFailedAttempts", "Từ 1 tới 100.");
        if (request.AttemptWindowMinutes is < 1 or > 10080)
            ModelState.AddModelError("attemptWindowMinutes", "Từ 1 tới 10080 phút (7 ngày).");
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        return PuzzleConfigResponse.From(await puzzles.UpdateAsync(puzzleId, request, ActorId(), ct));
    }

    [HttpPost("{puzzleId}/solutions")]
    public async Task<ActionResult<PuzzleSolutionResponse>> AddSolution(string puzzleId, AddPuzzleSolutionRequest request, CancellationToken ct)
    {
        if (request.Sha256 is null || !Sha256Pattern().IsMatch(request.Sha256))
            ModelState.AddModelError("sha256", "Bắt buộc, 64 ký tự hex chữ thường.");
        if (request.Note is { Length: > MaxNoteLength })
            ModelState.AddModelError("note", $"Tối đa {MaxNoteLength} ký tự.");
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var solution = await puzzles.AddSolutionAsync(puzzleId, request.Sha256!, request.Note?.Trim() ?? "", ActorId(), ct);
        return StatusCode(StatusCodes.Status201Created, PuzzleSolutionResponse.From(solution));
    }

    [HttpDelete("{puzzleId}/solutions/{solutionId:int}")]
    public async Task<IActionResult> DeleteSolution(string puzzleId, int solutionId, CancellationToken ct)
    {
        await puzzles.DeleteSolutionAsync(puzzleId, solutionId, ActorId(), ct);
        return NoContent();
    }

    [HttpGet("{puzzleId}/submissions")]
    public async Task<ActionResult<PuzzleSubmissionPage>> Submissions(
        string puzzleId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? result,
        [FromQuery] Guid? installationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (result is not null and not (SubmissionResult.Accepted or SubmissionResult.Rejected or SubmissionResult.Blocked))
            ModelState.AddModelError("result", "Là accepted, rejected hoặc blocked.");
        if (page < 1)
            ModelState.AddModelError("page", "Từ 1 trở lên.");
        if (pageSize is < 1 or > 200)
            ModelState.AddModelError("pageSize", "Từ 1 tới 200.");
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var range = DateRange.Resolve(from, to, clock);
        return await puzzles.ListSubmissionsAsync(puzzleId, range, result, installationId, page, pageSize, ct);
    }

    private Guid ActorId() => Guid.Parse(users.GetUserId(User)!);
}
