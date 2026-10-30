using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ThreeTwelve.Api.Tests;

public class AdminApiFactory : PuzzleApiFactory
{
    public const string AdminEmail = "admin@example.test";
    public const string AdminPassword = "mat-khau-thu-dai-12";

    protected virtual int AdminLoginPermitLimit => 1000;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Admin:Email", AdminEmail);
        builder.UseSetting("Admin:Password", AdminPassword);
        builder.UseSetting("RateLimits:AdminLogin:PermitLimit", AdminLoginPermitLimit.ToString());
    }

    public HttpClient CreateBrowser() =>
        CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), HandleCookies = true });

    public async Task<HttpClient> LoginAsync(string email = AdminEmail, string password = AdminPassword)
    {
        var browser = CreateBrowser();
        var response = await browser.PostAsJsonAsync("/api/admin/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        return browser;
    }

    public async Task<Guid> CreateAccountAsync(HttpClient admin, string email, string password = "mat-khau-moi-12345")
    {
        var response = await admin.PostAsJsonAsync("/api/admin/accounts", new { email, displayName = "Thử", password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetGuid();
    }

    public static string NewEmail() => $"user-{Guid.NewGuid():N}@example.test";
}

public sealed class TightAdminLoginApiFactory : AdminApiFactory
{
    protected override int AdminLoginPermitLimit => 2;
}
