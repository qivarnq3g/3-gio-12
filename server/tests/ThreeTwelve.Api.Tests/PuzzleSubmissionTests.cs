using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ThreeTwelve.Api.Tests;

public sealed class PuzzleSubmissionTests(PuzzleApiFactory factory) : IClassFixture<PuzzleApiFactory>
{
    private const string Correct = "3a7bd3e2360a3d29eea436fcfb7e44c735d117c42d1c1835420b6b9942dd4f1b";
    private const string Wrong = "0000000000000000000000000000000000000000000000000000000000000000";

    private static string Url(string puzzleId) => $"/api/v1/puzzles/{puzzleId}/submissions";

    private static object Submission(Guid installationId, string sha256) =>
        new { installationId, gameVersion = "0.3.0", sha256 };

    private DateTimeOffset Now => factory.Clock.GetUtcNow();

    [Fact]
    public async Task Correct_evidence_is_accepted_with_the_unlock_id_and_costs_no_attempt()
    {
        using var client = factory.CreateClient();
        var puzzleId = await factory.CreatePuzzleAsync(solutions: Correct);
        var installationId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync(Url(puzzleId), Submission(installationId, Correct));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("accepted", body.GetProperty("result").GetString());
        Assert.Equal($"ending_{puzzleId}", body.GetProperty("unlockId").GetString());
        Assert.Equal(5, body.GetProperty("attemptsRemaining").GetInt32());
        Assert.Equal(Now, body.GetProperty("windowResetsAt").GetDateTimeOffset());
        Assert.Equal(["accepted"], await factory.SubmissionResultsAsync(puzzleId, installationId));
    }

    [Fact]
    public async Task Any_of_several_solutions_is_accepted()
    {
        using var client = factory.CreateClient();
        var other = new string('a', 64);
        var puzzleId = await factory.CreatePuzzleAsync(solutions: [Correct, other]);

        var response = await client.PostAsJsonAsync(Url(puzzleId), Submission(Guid.NewGuid(), other));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("accepted", body.GetProperty("result").GetString());
    }

    [Fact]
    public async Task Wrong_evidence_is_rejected_without_an_unlock_id_and_costs_one_attempt()
    {
        using var client = factory.CreateClient();
        var puzzleId = await factory.CreatePuzzleAsync(windowMinutes: 60, solutions: Correct);
        var installationId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync(Url(puzzleId), Submission(installationId, Wrong));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("rejected", body.GetProperty("result").GetString());
        Assert.False(body.TryGetProperty("unlockId", out _));
        Assert.Equal(4, body.GetProperty("attemptsRemaining").GetInt32());
        Assert.Equal(Now.AddMinutes(60).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"), body.GetProperty("windowResetsAt").GetString());
        Assert.Equal(["rejected"], await factory.SubmissionResultsAsync(puzzleId, installationId));
    }

    [Fact]
    public async Task Once_attempts_run_out_even_correct_evidence_gets_429_until_the_oldest_failure_expires()
    {
        using var client = factory.CreateClient();
        var puzzleId = await factory.CreatePuzzleAsync(maxFailedAttempts: 2, windowMinutes: 60, solutions: Correct);
        var installationId = Guid.NewGuid();
        await client.PostAsJsonAsync(Url(puzzleId), Submission(installationId, Wrong));
        factory.Clock.Advance(TimeSpan.FromMinutes(10));
        await client.PostAsJsonAsync(Url(puzzleId), Submission(installationId, Wrong));
        factory.Clock.Advance(TimeSpan.FromMinutes(5));

        var blocked = await client.PostAsJsonAsync(Url(puzzleId), Submission(installationId, Correct));

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.Equal(TimeSpan.FromMinutes(45), blocked.Headers.RetryAfter?.Delta);
        Assert.Equal("application/problem+json", blocked.Content.Headers.ContentType?.MediaType);
        Assert.Equal(["rejected", "rejected", "blocked"], await factory.SubmissionResultsAsync(puzzleId, installationId));

        factory.Clock.Advance(TimeSpan.FromMinutes(45));
        var afterExpiry = await client.PostAsJsonAsync(Url(puzzleId), Submission(installationId, Correct));

        var body = await afterExpiry.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("accepted", body.GetProperty("result").GetString());
        Assert.Equal(1, body.GetProperty("attemptsRemaining").GetInt32());
        Assert.Equal(Now.AddMinutes(-50).AddMinutes(60), body.GetProperty("windowResetsAt").GetDateTimeOffset());
    }

    [Fact]
    public async Task Blocked_submissions_do_not_extend_the_block()
    {
        using var client = factory.CreateClient();
        var puzzleId = await factory.CreatePuzzleAsync(maxFailedAttempts: 1, windowMinutes: 30, solutions: Correct);
        var installationId = Guid.NewGuid();
        await client.PostAsJsonAsync(Url(puzzleId), Submission(installationId, Wrong));
        factory.Clock.Advance(TimeSpan.FromMinutes(20));
        await client.PostAsJsonAsync(Url(puzzleId), Submission(installationId, Wrong));
        factory.Clock.Advance(TimeSpan.FromMinutes(10));

        var response = await client.PostAsJsonAsync(Url(puzzleId), Submission(installationId, Correct));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Resending_correct_evidence_is_accepted_again()
    {
        using var client = factory.CreateClient();
        var puzzleId = await factory.CreatePuzzleAsync(maxFailedAttempts: 1, solutions: Correct);
        var installationId = Guid.NewGuid();

        await client.PostAsJsonAsync(Url(puzzleId), Submission(installationId, Correct));
        var again = await client.PostAsJsonAsync(Url(puzzleId), Submission(installationId, Correct));

        var body = await again.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("accepted", body.GetProperty("result").GetString());
        Assert.Equal(1, body.GetProperty("attemptsRemaining").GetInt32());
    }

    [Fact]
    public async Task Attempts_are_counted_per_installation()
    {
        using var client = factory.CreateClient();
        var puzzleId = await factory.CreatePuzzleAsync(maxFailedAttempts: 1, solutions: Correct);
        await client.PostAsJsonAsync(Url(puzzleId), Submission(Guid.NewGuid(), Wrong));

        var response = await client.PostAsJsonAsync(Url(puzzleId), Submission(Guid.NewGuid(), Wrong));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Lowering_the_limit_below_the_failures_waits_for_enough_of_them_to_expire()
    {
        using var client = factory.CreateClient();
        var puzzleId = await factory.CreatePuzzleAsync(maxFailedAttempts: 3, windowMinutes: 60, solutions: Correct);
        var installationId = Guid.NewGuid();
        for (var i = 0; i < 3; i++)
        {
            await client.PostAsJsonAsync(Url(puzzleId), Submission(installationId, Wrong));
            factory.Clock.Advance(TimeSpan.FromMinutes(10));
        }
        await factory.SetMaxFailedAttemptsAsync(puzzleId, 2);

        var response = await client.PostAsJsonAsync(Url(puzzleId), Submission(installationId, Correct));

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal(TimeSpan.FromMinutes(40), response.Headers.RetryAfter?.Delta);
    }

    [Fact]
    public async Task Parallel_wrong_submissions_never_get_past_the_limit()
    {
        using var client = factory.CreateClient();
        var puzzleId = await factory.CreatePuzzleAsync(maxFailedAttempts: 3, solutions: Correct);
        var installationId = Guid.NewGuid();

        var responses = await Task.WhenAll(Enumerable.Range(0, 10)
            .Select(_ => client.PostAsJsonAsync(Url(puzzleId), Submission(installationId, Wrong))));

        Assert.Equal(3, responses.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.Equal(7, responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests));
        var results = await factory.SubmissionResultsAsync(puzzleId, installationId);
        Assert.Equal(3, results.Count(r => r == "rejected"));
    }

    [Fact]
    public async Task The_story_puzzle_ships_disabled_so_it_answers_404()
    {
        using var client = factory.CreateClient();
        var installationId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync(Url("red-thread-sandal"), Submission(installationId, Correct));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Empty(await factory.SubmissionResultsAsync("red-thread-sandal", installationId));
    }

    [Fact]
    public async Task Unknown_or_disabled_puzzles_answer_404_and_store_nothing()
    {
        using var client = factory.CreateClient();
        var disabled = await factory.CreatePuzzleAsync(enabled: false, solutions: Correct);
        var installationId = Guid.NewGuid();

        var unknown = await client.PostAsJsonAsync(Url("no-such-puzzle"), Submission(installationId, Correct));
        var off = await client.PostAsJsonAsync(Url(disabled), Submission(installationId, Correct));

        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, off.StatusCode);
        Assert.Empty(await factory.SubmissionResultsAsync(disabled, installationId));
    }

    [Theory]
    [InlineData("""{"installationId":"6f1c2a9e-3b7d-4c51-9a2e-0d8f4b6e1a73","gameVersion":"0.3.0","sha256":"3A7BD3E2360A3D29EEA436FCFB7E44C735D117C42D1C1835420B6B9942DD4F1B"}""")]
    [InlineData("""{"installationId":"6f1c2a9e-3b7d-4c51-9a2e-0d8f4b6e1a73","gameVersion":"0.3.0","sha256":"3a7bd3"}""")]
    [InlineData("""{"installationId":"6f1c2a9e-3b7d-4c51-9a2e-0d8f4b6e1a73","gameVersion":"0.3.0"}""")]
    [InlineData("""{"gameVersion":"0.3.0","sha256":"3a7bd3e2360a3d29eea436fcfb7e44c735d117c42d1c1835420b6b9942dd4f1b"}""")]
    [InlineData("""{"installationId":"00000000-0000-0000-0000-000000000000","gameVersion":"0.3.0","sha256":"3a7bd3e2360a3d29eea436fcfb7e44c735d117c42d1c1835420b6b9942dd4f1b"}""")]
    [InlineData("""{"installationId":"6f1c2a9e-3b7d-4c51-9a2e-0d8f4b6e1a73","gameVersion":"beta","sha256":"3a7bd3e2360a3d29eea436fcfb7e44c735d117c42d1c1835420b6b9942dd4f1b"}""")]
    [InlineData("""{"installationId":"6f1c2a9e-3b7d-4c51-9a2e-0d8f4b6e1a73","gameVersion":"0.3.0","sha256":"3a7bd3e2360a3d29eea436fcfb7e44c735d117c42d1c1835420b6b9942dd4f1b","file":"x"}""")]
    public async Task Malformed_submissions_answer_400_and_store_nothing(string json)
    {
        using var client = factory.CreateClient();
        var puzzleId = await factory.CreatePuzzleAsync(solutions: Correct);

        var response = await client.PostAsync(Url(puzzleId), new StringContent(json, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await factory.SubmissionResultsAsync(puzzleId, Guid.Parse("6f1c2a9e-3b7d-4c51-9a2e-0d8f4b6e1a73")));
    }

    [Fact]
    public async Task A_puzzle_id_outside_the_contract_pattern_answers_400()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(Url("Red_Thread"), Submission(Guid.NewGuid(), Correct));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

public sealed class PuzzleRateLimitTests(TightPuzzleRateLimitApiFactory factory) : IClassFixture<TightPuzzleRateLimitApiFactory>
{
    [Fact]
    public async Task Answers_429_per_address_before_counting_attempts_and_stores_nothing()
    {
        using var client = factory.CreateClient();
        var puzzleId = await factory.CreatePuzzleAsync(solutions: new string('b', 64));
        var url = $"/api/v1/puzzles/{puzzleId}/submissions";
        var installationId = Guid.NewGuid();
        var submission = new { installationId, gameVersion = "0.3.0", sha256 = new string('c', 64) };

        await client.PostAsJsonAsync(url, submission);
        await client.PostAsJsonAsync(url, submission);
        var third = await client.PostAsJsonAsync(url, submission);

        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
        Assert.NotNull(third.Headers.RetryAfter?.Delta);
        Assert.Equal(2, (await factory.SubmissionResultsAsync(puzzleId, installationId)).Count);
    }
}
