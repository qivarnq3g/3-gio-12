using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ThreeTwelve.Api.Tests;

public sealed class AdminAuthTests(AdminApiFactory factory) : IClassFixture<AdminApiFactory>
{
    private const string LoginUrl = "/api/admin/auth/login";
    private const string MeUrl = "/api/admin/auth/me";

    [Fact]
    public async Task Seeded_admin_logs_in_with_a_strict_http_only_secure_cookie()
    {
        using var browser = factory.CreateBrowser();

        var response = await browser.PostAsJsonAsync(LoginUrl, new { email = AdminApiFactory.AdminEmail, password = AdminApiFactory.AdminPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(AdminApiFactory.AdminEmail, body.GetProperty("email").GetString());
        Assert.True(body.GetProperty("isActive").GetBoolean());
        Assert.EndsWith("Z", body.GetProperty("lastLoginAt").GetString());
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith("ThreeTwelve.Admin="));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync(MeUrl)).StatusCode);
    }

    [Fact]
    public async Task Wrong_password_and_unknown_email_get_the_same_401()
    {
        using var browser = factory.CreateBrowser();

        var wrongPassword = await browser.PostAsJsonAsync(LoginUrl, new { email = AdminApiFactory.AdminEmail, password = "sai-mat-khau-roi" });
        var unknownEmail = await browser.PostAsJsonAsync(LoginUrl, new { email = "nobody@example.test", password = "sai-mat-khau-roi" });

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
        var first = await wrongPassword.Content.ReadFromJsonAsync<JsonElement>();
        var second = await unknownEmail.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(first.GetProperty("title").GetString(), second.GetProperty("title").GetString());
        Assert.Equal(first.GetProperty("detail").GetString(), second.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Admin_api_without_a_session_answers_401_instead_of_redirecting()
    {
        using var browser = factory.CreateBrowser();

        var response = await browser.GetAsync(MeUrl);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.Unauthorized, (await browser.GetAsync("/api/admin/accounts")).StatusCode);
    }

    [Fact]
    public async Task Logout_ends_the_session()
    {
        using var browser = await factory.LoginAsync();

        var response = await browser.PostAsync("/api/admin/auth/logout", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await browser.GetAsync(MeUrl)).StatusCode);
    }

    [Fact]
    public async Task Five_wrong_passwords_in_a_row_block_even_the_right_one()
    {
        using var admin = await factory.LoginAsync();
        var email = AdminApiFactory.NewEmail();
        await factory.CreateAccountAsync(admin, email, "mat-khau-dung-12345");
        using var browser = factory.CreateBrowser();
        for (var i = 0; i < 5; i++)
            await browser.PostAsJsonAsync(LoginUrl, new { email, password = "sai-mat-khau-roi" });

        var response = await browser.PostAsJsonAsync(LoginUrl, new { email, password = "mat-khau-dung-12345" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_disabled_account_cannot_log_in()
    {
        using var admin = await factory.LoginAsync();
        var email = AdminApiFactory.NewEmail();
        var id = await factory.CreateAccountAsync(admin, email, "mat-khau-dung-12345");
        await admin.PatchAsJsonAsync($"/api/admin/accounts/{id}", new { isActive = false });
        using var browser = factory.CreateBrowser();

        var response = await browser.PostAsJsonAsync(LoginUrl, new { email, password = "mat-khau-dung-12345" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Disabling_an_account_ends_its_open_session_within_a_minute()
    {
        using var admin = await factory.LoginAsync();
        var email = AdminApiFactory.NewEmail();
        var id = await factory.CreateAccountAsync(admin, email, "mat-khau-dung-12345");
        using var other = await factory.LoginAsync(email, "mat-khau-dung-12345");

        await admin.PatchAsJsonAsync($"/api/admin/accounts/{id}", new { isActive = false });
        factory.Clock.Advance(TimeSpan.FromSeconds(61));

        Assert.Equal(HttpStatusCode.Unauthorized, (await other.GetAsync("/api/admin/accounts")).StatusCode);
    }

    [Fact]
    public async Task Changing_the_own_password_keeps_this_session_and_swaps_the_password()
    {
        using var admin = await factory.LoginAsync();
        var email = AdminApiFactory.NewEmail();
        await factory.CreateAccountAsync(admin, email, "mat-khau-cu-123456");
        using var browser = await factory.LoginAsync(email, "mat-khau-cu-123456");

        var wrongCurrent = await browser.PostAsJsonAsync("/api/admin/auth/password", new { currentPassword = "khong-phai-dau", newPassword = "mat-khau-moi-123456" });
        var tooShort = await browser.PostAsJsonAsync("/api/admin/auth/password", new { currentPassword = "mat-khau-cu-123456", newPassword = "ngan" });
        var changed = await browser.PostAsJsonAsync("/api/admin/auth/password", new { currentPassword = "mat-khau-cu-123456", newPassword = "mat-khau-moi-123456" });

        Assert.Equal(HttpStatusCode.BadRequest, wrongCurrent.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, tooShort.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync(MeUrl)).StatusCode);
        using var fresh = factory.CreateBrowser();
        Assert.Equal(HttpStatusCode.Unauthorized, (await fresh.PostAsJsonAsync(LoginUrl, new { email, password = "mat-khau-cu-123456" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await fresh.PostAsJsonAsync(LoginUrl, new { email, password = "mat-khau-moi-123456" })).StatusCode);
    }

    [Theory]
    [InlineData("""{"email":"admin@example.test"}""")]
    [InlineData("""{"email":"khong-phai-email","password":"x"}""")]
    [InlineData("""{"email":"admin@example.test","password":"mat-khau-thu-dai-12","remember":true}""")]
    public async Task Malformed_login_requests_answer_400(string json)
    {
        using var browser = factory.CreateBrowser();

        var response = await browser.PostAsync(LoginUrl, new StringContent(json, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

public sealed class AdminLoginRateLimitTests(TightAdminLoginApiFactory factory) : IClassFixture<TightAdminLoginApiFactory>
{
    [Fact]
    public async Task Answers_429_after_the_per_address_limit()
    {
        using var browser = factory.CreateBrowser();
        var wrong = new { email = AdminApiFactory.AdminEmail, password = "sai-mat-khau-roi" };

        await browser.PostAsJsonAsync("/api/admin/auth/login", wrong);
        await browser.PostAsJsonAsync("/api/admin/auth/login", wrong);
        var third = await browser.PostAsJsonAsync("/api/admin/auth/login", wrong);

        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
        Assert.NotNull(third.Headers.RetryAfter?.Delta);
    }
}
