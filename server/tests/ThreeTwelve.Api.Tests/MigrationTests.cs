using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using ThreeTwelve.Api.Data;

namespace ThreeTwelve.Api.Tests;

public sealed class MigrationTests(PostgresApiFactory factory) : IClassFixture<PostgresApiFactory>
{
    [Fact]
    public async Task Startup_applies_every_migration()
    {
        _ = factory.Server;
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var pending = await db.Database.GetPendingMigrationsAsync();
        var applied = await db.Database.GetAppliedMigrationsAsync();

        Assert.Empty(pending);
        Assert.NotEmpty(applied);
    }

    [Fact]
    public async Task Dashboard_index_is_on_received_at_and_type()
    {
        _ = factory.Server;
        await using var connection = new NpgsqlConnection(factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "select indexdef from pg_indexes where tablename = 'telemetry_events'", connection);
        var definitions = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            definitions.Add(reader.GetString(0));

        Assert.Contains(definitions, d => d.Contains("(received_at, type)"));
        Assert.DoesNotContain(definitions, d => d.Contains("occurred_at"));
    }
}
