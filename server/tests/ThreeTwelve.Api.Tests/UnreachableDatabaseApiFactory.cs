using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ThreeTwelve.Api.Tests;

public sealed class UnreachableDatabaseApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Database", "Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=2");
        builder.UseSetting("Database:MigrateOnStartup", "false");
    }
}
