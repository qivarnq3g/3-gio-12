using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ThreeTwelve.Api.Domain;

namespace ThreeTwelve.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityUserContext<AdminUser, Guid>(options)
{
    public DbSet<TelemetryEvent> TelemetryEvents => Set<TelemetryEvent>();
    public DbSet<Puzzle> Puzzles => Set<Puzzle>();
    public DbSet<PuzzleSolution> PuzzleSolutions => Set<PuzzleSolution>();
    public DbSet<PuzzleSubmission> PuzzleSubmissions => Set<PuzzleSubmission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AdminUser>(e =>
        {
            e.ToTable("admin_users");
            e.Property(x => x.DisplayName).HasMaxLength(64);
            e.Property(x => x.IsActive).HasDefaultValue(true);
        });
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("admin_user_claims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("admin_user_logins");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("admin_user_tokens");

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

        modelBuilder.Entity<Puzzle>(e =>
        {
            e.ToTable(t =>
            {
                t.HasCheckConstraint("ck_puzzles_max_failed_attempts", "max_failed_attempts between 1 and 100");
                t.HasCheckConstraint("ck_puzzles_attempt_window_minutes", "attempt_window_minutes between 1 and 10080");
            });
            e.Property(x => x.Id).HasMaxLength(64);
            e.Property(x => x.Title).HasMaxLength(128);
            e.Property(x => x.UnlockId).HasMaxLength(64);
            e.Property(x => x.IsEnabled).HasDefaultValue(false);
            e.Property(x => x.MaxFailedAttempts).HasDefaultValue(5);
            e.Property(x => x.AttemptWindowMinutes).HasDefaultValue(60);
            e.HasMany(x => x.Solutions).WithOne().HasForeignKey(x => x.PuzzleId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<AdminUser>().WithMany().HasForeignKey(x => x.UpdatedById).OnDelete(DeleteBehavior.SetNull);
            e.HasData(new Puzzle
            {
                Id = "red-thread-sandal",
                Title = "Đôi dép chỉ đỏ",
                UnlockId = "ending_red_thread_sandal",
                IsEnabled = false,
                MaxFailedAttempts = 5,
                AttemptWindowMinutes = 60,
                UpdatedAt = new DateTimeOffset(2026, 10, 30, 0, 0, 0, TimeSpan.Zero),
            });
        });

        modelBuilder.Entity<PuzzleSolution>(e =>
        {
            e.ToTable(t => t.HasCheckConstraint("ck_puzzle_solutions_sha256", "sha256 ~ '^[0-9a-f]{64}$'"));
            e.Property(x => x.PuzzleId).HasMaxLength(64);
            e.Property(x => x.Sha256).HasColumnType("character(64)");
            e.Property(x => x.Note).HasMaxLength(200).HasDefaultValue("");
            e.HasIndex(x => new { x.PuzzleId, x.Sha256 }).IsUnique();
            e.HasOne<AdminUser>().WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PuzzleSubmission>(e =>
        {
            e.Property(x => x.PuzzleId).HasMaxLength(64);
            e.Property(x => x.Sha256).HasColumnType("character(64)");
            e.Property(x => x.Result).HasMaxLength(16);
            e.Property(x => x.GameVersion).HasMaxLength(16);
            e.HasOne<Puzzle>().WithMany().HasForeignKey(x => x.PuzzleId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.PuzzleId, x.InstallationId, x.SubmittedAt });
            e.HasIndex(x => new { x.PuzzleId, x.SubmittedAt });
        });
    }
}
