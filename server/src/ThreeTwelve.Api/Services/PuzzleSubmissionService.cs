using Microsoft.EntityFrameworkCore;
using ThreeTwelve.Api.Data;
using ThreeTwelve.Api.Domain;

namespace ThreeTwelve.Api.Services;

public enum SubmissionOutcomeKind
{
    Accepted,
    Rejected,
    Blocked,
    PuzzleNotFound,
}

public sealed record SubmissionOutcome(
    SubmissionOutcomeKind Kind,
    string? UnlockId = null,
    int AttemptsRemaining = 0,
    DateTimeOffset WindowResetsAt = default,
    TimeSpan? RetryAfter = null);

public interface IPuzzleSubmissionService
{
    Task<SubmissionOutcome> SubmitAsync(string puzzleId, Guid installationId, string gameVersion, string sha256, CancellationToken ct);
}

public sealed class PuzzleSubmissionService(AppDbContext db, TimeProvider clock) : IPuzzleSubmissionService
{
    public async Task<SubmissionOutcome> SubmitAsync(string puzzleId, Guid installationId, string gameVersion, string sha256, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var puzzle = await db.Puzzles.AsNoTracking()
            .Where(p => p.Id == puzzleId && p.IsEnabled)
            .Select(p => new { p.UnlockId, p.MaxFailedAttempts, p.AttemptWindowMinutes })
            .SingleOrDefaultAsync(ct);
        if (puzzle is null)
            return new SubmissionOutcome(SubmissionOutcomeKind.PuzzleNotFound);

        var lockKey = $"{puzzleId}:{installationId}";
        await db.Database.ExecuteSqlAsync($"select pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", ct);

        var now = clock.GetUtcNow();
        var window = TimeSpan.FromMinutes(puzzle.AttemptWindowMinutes);
        var windowStart = now - window;
        var failures = await db.PuzzleSubmissions.AsNoTracking()
            .Where(s => s.PuzzleId == puzzleId
                && s.InstallationId == installationId
                && s.Result == SubmissionResult.Rejected
                && s.SubmittedAt > windowStart)
            .OrderBy(s => s.SubmittedAt)
            .Select(s => s.SubmittedAt)
            .ToListAsync(ct);

        SubmissionOutcome outcome;
        string result;
        if (failures.Count >= puzzle.MaxFailedAttempts)
        {
            var attemptReturnsAt = failures[failures.Count - puzzle.MaxFailedAttempts] + window;
            result = SubmissionResult.Blocked;
            outcome = new SubmissionOutcome(SubmissionOutcomeKind.Blocked,
                WindowResetsAt: failures[^1] + window,
                RetryAfter: attemptReturnsAt - now);
        }
        else if (await db.PuzzleSolutions.AnyAsync(s => s.PuzzleId == puzzleId && s.Sha256 == sha256, ct))
        {
            result = SubmissionResult.Accepted;
            outcome = new SubmissionOutcome(SubmissionOutcomeKind.Accepted, puzzle.UnlockId,
                puzzle.MaxFailedAttempts - failures.Count,
                failures.Count > 0 ? failures[^1] + window : now);
        }
        else
        {
            result = SubmissionResult.Rejected;
            outcome = new SubmissionOutcome(SubmissionOutcomeKind.Rejected, null,
                puzzle.MaxFailedAttempts - failures.Count - 1,
                now + window);
        }

        db.PuzzleSubmissions.Add(new PuzzleSubmission
        {
            PuzzleId = puzzleId,
            InstallationId = installationId,
            Sha256 = sha256,
            Result = result,
            GameVersion = gameVersion,
            SubmittedAt = now,
        });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return outcome;
    }
}
