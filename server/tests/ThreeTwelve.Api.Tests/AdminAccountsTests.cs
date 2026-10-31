using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ThreeTwelve.Api.Tests;

public sealed class AdminAccountsTests(AdminApiFactory factory) : IClassFixture<AdminApiFactory>
{
    private const string AccountsUrl = "/api/admin/accounts";

    [Fact]
    public async Task Creates_an_account_that_shows_in_the_list_and_can_log_in()
    {
        using var admin = await factory.LoginAsync();
        var email = AdminApiFactory.NewEmail();

        var response = await admin.PostAsJsonAsync(AccountsUrl, new { email, displayName = "Thắng", password = "mat-khau-moi-12345" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Thắng", created.GetProperty("displayName").GetString());
        Assert.Equal(JsonValueKind.Null, created.GetProperty("lastLoginAt").ValueKind);
        var list = await admin.GetFromJsonAsync<JsonElement>(AccountsUrl);
        Assert.Contains(list.EnumerateArray(), a => a.GetProperty("email").GetString() == email);
        using var _ = await factory.LoginAsync(email, "mat-khau-moi-12345");
    }

    [Fact]
    public async Task An_email_that_already_has_an_account_answers_409()
    {
        using var admin = await factory.LoginAsync();

        var response = await admin.PostAsJsonAsync(AccountsUrl, new { email = AdminApiFactory.AdminEmail, displayName = "Trùng", password = "mat-khau-moi-12345" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("khong-phai-email", "Tên", "mat-khau-moi-12345")]
    [InlineData("a@example.test", "", "mat-khau-moi-12345")]
    [InlineData("a@example.test", "Tên", "ngan-qua")]
    public async Task Invalid_new_accounts_answer_400(string email, string displayName, string password)
    {
        using var admin = await factory.LoginAsync();

        var response = await admin.PostAsJsonAsync(AccountsUrl, new { email, displayName, password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Renames_and_reenables_an_account()
    {
        using var admin = await factory.LoginAsync();
        var id = await factory.CreateAccountAsync(admin, AdminApiFactory.NewEmail());
        await admin.PatchAsJsonAsync($"{AccountsUrl}/{id}", new { isActive = false });

        var response = await admin.PatchAsJsonAsync($"{AccountsUrl}/{id}", new { displayName = "Tên mới", isActive = true });

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Tên mới", body.GetProperty("displayName").GetString());
        Assert.True(body.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task Cannot_disable_or_delete_yourself()
    {
        using var admin = await factory.LoginAsync();
        var me = (await admin.GetFromJsonAsync<JsonElement>("/api/admin/auth/me")).GetProperty("id").GetGuid();

        var disable = await admin.PatchAsJsonAsync($"{AccountsUrl}/{me}", new { isActive = false });
        var delete = await admin.DeleteAsync($"{AccountsUrl}/{me}");

        Assert.Equal(HttpStatusCode.Conflict, disable.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, delete.StatusCode);
    }

    [Fact]
    public async Task Deletes_another_account()
    {
        using var admin = await factory.LoginAsync();
        var email = AdminApiFactory.NewEmail();
        var id = await factory.CreateAccountAsync(admin, email);

        var response = await admin.DeleteAsync($"{AccountsUrl}/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var list = await admin.GetFromJsonAsync<JsonElement>(AccountsUrl);
        Assert.DoesNotContain(list.EnumerateArray(), a => a.GetProperty("email").GetString() == email);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync($"{AccountsUrl}/{id}")).StatusCode);
    }

    [Fact]
    public async Task Resetting_another_password_swaps_it_but_not_your_own()
    {
        using var admin = await factory.LoginAsync();
        var email = AdminApiFactory.NewEmail();
        var id = await factory.CreateAccountAsync(admin, email, "mat-khau-cu-123456");
        var me = (await admin.GetFromJsonAsync<JsonElement>("/api/admin/auth/me")).GetProperty("id").GetGuid();

        var reset = await admin.PostAsJsonAsync($"{AccountsUrl}/{id}/password", new { newPassword = "mat-khau-dat-lai-12" });
        var own = await admin.PostAsJsonAsync($"{AccountsUrl}/{me}/password", new { newPassword = "mat-khau-dat-lai-12" });

        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, own.StatusCode);
        using var fresh = factory.CreateBrowser();
        Assert.Equal(HttpStatusCode.Unauthorized, (await fresh.PostAsJsonAsync("/api/admin/auth/login", new { email, password = "mat-khau-cu-123456" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await fresh.PostAsJsonAsync("/api/admin/auth/login", new { email, password = "mat-khau-dat-lai-12" })).StatusCode);
    }

    [Fact]
    public async Task Unknown_accounts_answer_404_and_empty_updates_400()
    {
        using var admin = await factory.LoginAsync();
        var id = await factory.CreateAccountAsync(admin, AdminApiFactory.NewEmail());

        var unknown = await admin.PatchAsJsonAsync($"{AccountsUrl}/{Guid.NewGuid()}", new { displayName = "X" });
        var empty = await admin.PatchAsJsonAsync($"{AccountsUrl}/{id}", new { });
        var notUuid = await admin.DeleteAsync($"{AccountsUrl}/abc");

        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, notUuid.StatusCode);
    }
}
