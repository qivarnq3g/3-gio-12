using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Npgsql;

namespace ThreeTwelve.Api.Tests;

public class PuzzleApiFactory : PostgresApiFactory
{
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 12, 2, 14, 0, 0, TimeSpan.Zero));

    protected virtual int PuzzleSubmitPermitLimit => 1000;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("RateLimits:PuzzleSubmit:PermitLimit", PuzzleSubmitPermitLimit.ToString());
        builder.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(Clock));
    }

    public async Task<string> CreatePuzzleAsync(int maxFailedAttempts = 5, int windowMinutes = 60, bool enabled = true, params string[] solutions)
    {
        _ = Server;
        var puzzleId = $"test-{Guid.NewGuid():N}"[..20];
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using (var insert = new NpgsqlCommand("""
            insert into puzzles (id, title, unlock_id, is_enabled, max_failed_attempts, attempt_window_minutes, updated_at)
            values (@id, 'Test', @unlock, @enabled, @max, @window, now())
            """, connection))
        {
            insert.Parameters.AddWithValue("id", puzzleId);
            insert.Parameters.AddWithValue("unlock", $"ending_{puzzleId}");
            insert.Parameters.AddWithValue("enabled", enabled);
            insert.Parameters.AddWithValue("max", maxFailedAttempts);
            insert.Parameters.AddWithValue("window", windowMinutes);
            await insert.ExecuteNonQueryAsync();
        }

        foreach (var sha256 in solutions)
        {
            await using var solution = new NpgsqlCommand(
                "insert into puzzle_solutions (puzzle_id, sha256, created_at) values (@id, @sha, now())", connection);
            solution.Parameters.AddWithValue("id", puzzleId);
            solution.Parameters.AddWithValue("sha", sha256);
            await solution.ExecuteNonQueryAsync();
        }

        return puzzleId;
    }

    public async Task SetMaxFailedAttemptsAsync(string puzzleId, int maxFailedAttempts)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "update puzzles set max_failed_attempts = @max where id = @id", connection);
        command.Parameters.AddWithValue("id", puzzleId);
        command.Parameters.AddWithValue("max", maxFailedAttempts);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<string>> SubmissionResultsAsync(string puzzleId, Guid installationId)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "select result from puzzle_submissions where puzzle_id = @id and installation_id = @installation order by id", connection);
        command.Parameters.AddWithValue("id", puzzleId);
        command.Parameters.AddWithValue("installation", installationId);
        var results = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            results.Add(reader.GetString(0));
        return results;
    }
}

public sealed class TightPuzzleRateLimitApiFactory : PuzzleApiFactory
{
    protected override int PuzzleSubmitPermitLimit => 2;
}
