using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ThreeTwelve.Api.Tests;

public sealed class AdminPuzzlesTests(AdminApiFactory factory) : IClassFixture<AdminApiFactory>
{
    private const string A = "3a7bd3e2360a3d29eea436fcfb7e44c735d117c42d1c1835420b6b9942dd4f1b";
    private const string B = "1111111111111111111111111111111111111111111111111111111111111111";
    private const string Wrong = "0000000000000000000000000000000000000000000000000000000000000000";

    private static string Admin(string puzzleId) => $"/api/admin/puzzles/{puzzleId}";
    private static string Game(string puzzleId) => $"/api/v1/puzzles/{puzzleId}/submissions";
    private static object Submission(Guid installationId, string sha256) => new { installationId, gameVersion = "0.3.0", sha256 };

    [Fact]
    public async Task Puzzle_api_needs_a_session()
    {
        using var browser = factory.CreateBrowser();
        Assert.Equal(HttpStatusCode.Unauthorized, (await browser.GetAsync("/api/admin/puzzles")).StatusCode);
    }

    [Fact]
    public async Task Lists_the_seeded_puzzle_and_returns_its_configuration()
    {
        using var admin = await factory.LoginAsync();

        var list = await admin.GetFromJsonAsync<JsonElement>("/api/admin/puzzles");
        var seeded = list.EnumerateArray().Single(p => p.GetProperty("puzzleId").GetString() == "red-thread-sandal");
        Assert.Equal("ending_red_thread_sandal", seeded.GetProperty("unlockId").GetString());
        Assert.Equal(JsonValueKind.Array, seeded.GetProperty("solutions").ValueKind);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync(Admin("khong-co"))).StatusCode);
    }

    [Fact]
    public async Task A_hash_added_on_the_admin_page_is_accepted_by_the_game_and_removing_it_rejects_again()
    {
        using var admin = await factory.LoginAsync();
        using var game = factory.CreateClient();
        var puzzleId = await factory.CreatePuzzleAsync(enabled: true, solutions: A);

        Assert.Equal("rejected", await ResultAsync(game, puzzleId, B));

        var added = await admin.PostAsJsonAsync($"{Admin(puzzleId)}/solutions", new { sha256 = B, note = "bản quét lại" });
        Assert.Equal(HttpStatusCode.Created, added.StatusCode);
        var solution = await added.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("bản quét lại", solution.GetProperty("note").GetString());
        Assert.Equal("accepted", await ResultAsync(game, puzzleId, B));

        var removed = await admin.DeleteAsync($"{Admin(puzzleId)}/solutions/{solution.GetProperty("id").GetInt32()}");
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        Assert.Equal("rejected", await ResultAsync(game, puzzleId, B));
    }

    [Fact]
    public async Task Lowering_the_attempt_limit_applies_to_the_next_game_submission()
    {
        using var admin = await factory.LoginAsync();
        using var game = factory.CreateClient();
        var puzzleId = await factory.CreatePuzzleAsync(maxFailedAttempts: 5, enabled: true, solutions: A);
        var installation = Guid.NewGuid();
        await game.PostAsJsonAsync(Game(puzzleId), Submission(installation, Wrong));

        var patched = await admin.PatchAsJsonAsync(Admin(puzzleId), new { maxFailedAttempts = 1 });

        Assert.Equal(HttpStatusCode.OK, patched.StatusCode);
        Assert.Equal(1, (await patched.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("maxFailedAttempts").GetInt32());
        var next = await game.PostAsJsonAsync(Game(puzzleId), Submission(installation, A));
        Assert.Equal(HttpStatusCode.TooManyRequests, next.StatusCode);
    }

    [Fact]
    public async Task Disabling_hides_the_puzzle_from_the_game_and_enabling_brings_it_back()
    {
        using var admin = await factory.LoginAsync();
        using var game = factory.CreateClient();
        var puzzleId = await factory.CreatePuzzleAsync(enabled: true, solutions: A);

        await admin.PatchAsJsonAsync(Admin(puzzleId), new { isEnabled = false });
        Assert.Equal(HttpStatusCode.NotFound, (await game.PostAsJsonAsync(Game(puzzleId), Submission(Guid.NewGuid(), A))).StatusCode);

        await admin.PatchAsJsonAsync(Admin(puzzleId), new { isEnabled = true });
        Assert.Equal("accepted", await ResultAsync(game, puzzleId, A));
    }

    [Fact]
    public async Task Cannot_enable_without_a_hash_nor_remove_the_last_hash_of_an_enabled_puzzle()
    {
        using var admin = await factory.LoginAsync();
        var empty = await factory.CreatePuzzleAsync(enabled: false);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PatchAsJsonAsync(Admin(empty), new { isEnabled = true })).StatusCode);

        var enabled = await factory.CreatePuzzleAsync(enabled: true, solutions: A);
        var config = await admin.GetFromJsonAsync<JsonElement>(Admin(enabled));
        var onlyId = config.GetProperty("solutions")[0].GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.Conflict, (await admin.DeleteAsync($"{Admin(enabled)}/solutions/{onlyId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"{Admin(enabled)}/solutions", new { sha256 = A })).StatusCode);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"maxFailedAttempts\":0}")]
    [InlineData("{\"attemptWindowMinutes\":10081}")]
    [InlineData("{\"title\":\"x\"}")]
    public async Task Bad_update_bodies_answer_400(string body)
    {
        using var admin = await factory.LoginAsync();
        var puzzleId = await factory.CreatePuzzleAsync(enabled: false);

        var response = await admin.PatchAsync(Admin(puzzleId), new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Bad_hash_answers_400()
    {
        using var admin = await factory.LoginAsync();
        var puzzleId = await factory.CreatePuzzleAsync(enabled: false);

        var response = await admin.PostAsJsonAsync($"{Admin(puzzleId)}/solutions", new { sha256 = A.ToUpperInvariant() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Submission_history_is_newest_first_filterable_and_paged()
    {
        using var admin = await factory.LoginAsync();
        using var game = factory.CreateClient();
        var puzzleId = await factory.CreatePuzzleAsync(enabled: true, solutions: A);
        var installation = Guid.NewGuid();
        await game.PostAsJsonAsync(Game(puzzleId), Submission(installation, Wrong));
        factory.Clock.Advance(TimeSpan.FromSeconds(1));
        await game.PostAsJsonAsync(Game(puzzleId), Submission(installation, A));
        factory.Clock.Advance(TimeSpan.FromSeconds(1));
        await game.PostAsJsonAsync(Game(puzzleId), Submission(Guid.NewGuid(), Wrong));
        factory.Clock.Advance(TimeSpan.FromSeconds(1));

        var all = await admin.GetFromJsonAsync<JsonElement>($"{Admin(puzzleId)}/submissions?pageSize=2");
        Assert.Equal(3, all.GetProperty("total").GetInt32());
        Assert.Equal(2, all.GetProperty("items").GetArrayLength());
        Assert.Equal("rejected", all.GetProperty("items")[0].GetProperty("result").GetString());
        Assert.Equal("accepted", all.GetProperty("items")[1].GetProperty("result").GetString());

        var accepted = await admin.GetFromJsonAsync<JsonElement>($"{Admin(puzzleId)}/submissions?result=accepted");
        Assert.Equal(1, accepted.GetProperty("total").GetInt32());
        var mine = await admin.GetFromJsonAsync<JsonElement>($"{Admin(puzzleId)}/submissions?installationId={installation}");
        Assert.Equal(2, mine.GetProperty("total").GetInt32());
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync($"{Admin(puzzleId)}/submissions?result=maybe")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync($"{Admin(puzzleId)}/submissions?pageSize=201")).StatusCode);
    }

    private static async Task<string> ResultAsync(HttpClient game, string puzzleId, string sha256)
    {
        var response = await game.PostAsJsonAsync(Game(puzzleId), Submission(Guid.NewGuid(), sha256));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("result").GetString()!;
    }
}
