using Microsoft.EntityFrameworkCore;
using Npgsql;
using ThreeTwelve.Api.Contracts;
using ThreeTwelve.Api.Data;
using ThreeTwelve.Api.Domain;

namespace ThreeTwelve.Api.Services;

public interface IAdminPuzzleService
{
    Task<List<Puzzle>> ListAsync(CancellationToken ct);
    Task<Puzzle> GetAsync(string puzzleId, CancellationToken ct);
    Task<Puzzle> UpdateAsync(string puzzleId, UpdatePuzzleRequest request, Guid actorId, CancellationToken ct);
    Task<PuzzleSolution> AddSolutionAsync(string puzzleId, string sha256, string note, Guid actorId, CancellationToken ct);
    Task DeleteSolutionAsync(string puzzleId, int solutionId, Guid actorId, CancellationToken ct);
    Task<PuzzleSubmissionPage> ListSubmissionsAsync(string puzzleId, DateRange range, string? result, Guid? installationId, int page, int pageSize, CancellationToken ct);
}

// Puzzles are seeded by migration; the admin page only configures them (docs/api/openapi.yaml, admin-puzzles).
// The game's submission endpoint reads this configuration on every request, so changes apply to the next submission.
public sealed class AdminPuzzleService(AppDbContext db, TimeProvider clock) : IAdminPuzzleService
{
    public Task<List<Puzzle>> ListAsync(CancellationToken ct) =>
        db.Puzzles.AsNoTracking().Include(p => p.Solutions).OrderBy(p => p.Id).ToListAsync(ct);

    public async Task<Puzzle> GetAsync(string puzzleId, CancellationToken ct) =>
        await db.Puzzles.AsNoTracking().Include(p => p.Solutions).SingleOrDefaultAsync(p => p.Id == puzzleId, ct)
        ?? throw new NotFoundException("Không có câu đố này.");

    public async Task<Puzzle> UpdateAsync(string puzzleId, UpdatePuzzleRequest request, Guid actorId, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var puzzle = await LoadForUpdateAsync(puzzleId, ct);
        if (request.IsEnabled == true && puzzle.Solutions.Count == 0)
            throw new ConflictException("Câu đố chưa có mã băm hợp lệ nào; thêm mã băm trước khi bật.");

        if (request.IsEnabled is { } enabled) puzzle.IsEnabled = enabled;
        if (request.MaxFailedAttempts is { } max) puzzle.MaxFailedAttempts = max;
        if (request.AttemptWindowMinutes is { } window) puzzle.AttemptWindowMinutes = window;
        Touch(puzzle, actorId);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return puzzle;
    }

    public async Task<PuzzleSolution> AddSolutionAsync(string puzzleId, string sha256, string note, Guid actorId, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var puzzle = await LoadForUpdateAsync(puzzleId, ct);
        if (puzzle.Solutions.Any(s => s.Sha256 == sha256))
            throw new ConflictException("Mã băm này đã có trong câu đố.");

        var solution = new PuzzleSolution
        {
            PuzzleId = puzzleId,
            Sha256 = sha256,
            Note = note,
            CreatedAt = clock.GetUtcNow(),
            CreatedById = actorId,
        };
        puzzle.Solutions.Add(solution);
        Touch(puzzle, actorId);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException("Mã băm này đã có trong câu đố.");
        }
        await transaction.CommitAsync(ct);
        return solution;
    }

    public async Task DeleteSolutionAsync(string puzzleId, int solutionId, Guid actorId, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var puzzle = await LoadForUpdateAsync(puzzleId, ct);
        var solution = puzzle.Solutions.SingleOrDefault(s => s.Id == solutionId)
            ?? throw new NotFoundException("Không có mã băm này trong câu đố.");
        if (puzzle.IsEnabled && puzzle.Solutions.Count == 1)
            throw new ConflictException("Không bỏ được mã băm cuối cùng của câu đố đang bật; tắt câu đố trước.");

        db.PuzzleSolutions.Remove(solution);
        Touch(puzzle, actorId);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task<PuzzleSubmissionPage> ListSubmissionsAsync(string puzzleId, DateRange range, string? result, Guid? installationId, int page, int pageSize, CancellationToken ct)
    {
        if (!await db.Puzzles.AnyAsync(p => p.Id == puzzleId, ct))
            throw new NotFoundException("Không có câu đố này.");

        var query = db.PuzzleSubmissions.AsNoTracking()
            .Where(s => s.PuzzleId == puzzleId && s.SubmittedAt >= range.From && s.SubmittedAt < range.To);
        if (result is not null) query = query.Where(s => s.Result == result);
        if (installationId is { } id) query = query.Where(s => s.InstallationId == id);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(s => s.SubmittedAt).ThenByDescending(s => s.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);
        return new PuzzleSubmissionPage(items.Select(PuzzleSubmissionRecord.From).ToList(), page, pageSize, total);
    }

    // Called inside the caller's transaction. The row lock serialises enable/disable against
    // solution add/remove, so an enabled puzzle can never be left without a valid hash.
    private async Task<Puzzle> LoadForUpdateAsync(string puzzleId, CancellationToken ct)
    {
        var locked = await db.Database
            .SqlQuery<int>($"select 1 as \"Value\" from puzzles where id = {puzzleId} for update")
            .ToListAsync(ct);
        if (locked.Count == 0)
            throw new NotFoundException("Không có câu đố này.");
        return await db.Puzzles.Include(p => p.Solutions).SingleAsync(p => p.Id == puzzleId, ct);
    }

    private void Touch(Puzzle puzzle, Guid actorId)
    {
        puzzle.UpdatedAt = clock.GetUtcNow();
        puzzle.UpdatedById = actorId;
    }
}
