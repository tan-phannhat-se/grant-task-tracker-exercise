using GrantTracker.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GrantTracker.Api.Data;

/// <summary>
/// The EF Core session for grants and their tasks.
/// </summary>
public class GrantTrackerDbContext : DbContext
{
    /// <summary>
    /// Creates the context with the options registered at start-up (or built by a test).
    /// </summary>
    public GrantTrackerDbContext(DbContextOptions<GrantTrackerDbContext> options)
        : base(options)
    {
    }

    public DbSet<Grant> Grants => Set<Grant>();
    public DbSet<GrantTask> GrantTasks => Set<GrantTask>();

    /// <summary>
    /// Maps the domain classes to the Grants and GrantTasks tables.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureGrant(modelBuilder.Entity<Grant>());
        ConfigureGrantTask(modelBuilder.Entity<GrantTask>());
    }

    private static void ConfigureGrant(EntityTypeBuilder<Grant> grant)
    {
        grant.ToTable("Grants");
        grant.Property(g => g.ReferenceNumber).HasMaxLength(Grant.ReferenceNumberMaxLength);
        grant.Property(g => g.OrganisationName).HasMaxLength(Grant.OrganisationNameMaxLength);
        grant.Property(g => g.Status).HasConversion<int>();
        grant.HasIndex(g => g.ReferenceNumber).IsUnique();

        // EF fills the private list, so Grant.Tasks stays read-only and tasks are only added through AddTask.
        grant.HasMany(g => g.Tasks)
            .WithOne()
            .HasForeignKey(t => t.GrantId)
            .OnDelete(DeleteBehavior.Restrict); // tasks are history: never removed silently with their grant
        grant.Navigation(g => g.Tasks).HasField("_tasks");
    }

    private static void ConfigureGrantTask(EntityTypeBuilder<GrantTask> task)
    {
        task.ToTable("GrantTasks");
        task.Property(t => t.Title).HasMaxLength(GrantTask.TitleMaxLength);
        task.Property(t => t.Priority).HasConversion<int>();
        task.Property(t => t.IdempotencyKey).HasMaxLength(GrantTask.IdempotencyKeyLength);

        // SQLite drops DateTimeKind, so CreatedAt is marked UTC again when it is read.
        task.Property(t => t.CreatedAt)
            .HasConversion(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

        // Unique so one retried request can create only one task, even when two retries arrive together.
        task.HasIndex(t => t.IdempotencyKey).IsUnique().HasFilter("\"IdempotencyKey\" IS NOT NULL");
        task.HasIndex(t => t.GrantId);
        task.HasIndex(t => t.DueDate); // the overdue list filters on it
    }
}
