using Microsoft.EntityFrameworkCore;
using ThreeTwelve.Api.Domain;

namespace ThreeTwelve.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TelemetryEvent> TelemetryEvents => Set<TelemetryEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TelemetryEvent>(e =>
        {
            e.HasKey(x => x.EventId);
            e.Property(x => x.EventId).ValueGeneratedNever();
            e.Property(x => x.Type).HasMaxLength(32);
            e.Property(x => x.GameVersion).HasMaxLength(16);
            e.Property(x => x.AreaId).HasMaxLength(64);
            e.Property(x => x.CheckpointId).HasMaxLength(64);
            e.Property(x => x.ChoiceId).HasMaxLength(8);
            e.Property(x => x.EndingId).HasMaxLength(32);
            e.Property(x => x.EndReason).HasMaxLength(16);
            e.HasIndex(x => x.InstallationId);
            e.HasIndex(x => new { x.ReceivedAt, x.Type });
        });
    }
}
