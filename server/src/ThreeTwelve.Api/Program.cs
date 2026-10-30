using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ThreeTwelve.Api.Auth;
using ThreeTwelve.Api.Controllers;
using ThreeTwelve.Api.Data;
using ThreeTwelve.Api.Health;
using ThreeTwelve.Api.RateLimiting;
using ThreeTwelve.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database"))
        .UseSnakeCaseNamingConvention());
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ITelemetryIngestService, TelemetryIngestService>();
builder.Services.AddScoped<IPuzzleSubmissionService, PuzzleSubmissionService>();
builder.Services.AddScoped<IAdminAccountService, AdminAccountService>();
builder.Services.AddAdminAuth();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ServiceExceptionHandler>();
builder.Services.AddPublicRateLimits(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    await AdminSeeder.SeedAsync(scope.ServiceProvider, app.Configuration, app.Logger);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthResponseWriter.WriteAsync,
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
    },
});
app.UseExceptionHandler();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;
