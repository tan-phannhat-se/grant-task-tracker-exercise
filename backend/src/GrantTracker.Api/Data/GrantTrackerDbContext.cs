using GrantTracker.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace GrantTracker.Api.Data;

/// <summary>
/// The EF Core session for grants and their tasks.
/// </summary>
public class GrantTrackerDbContext : DbContext
{
    public GrantTrackerDbContext(DbContextOptions<GrantTrackerDbContext> options)
        : base(options)
    {
    }

    public DbSet<Grant> Grants => Set<Grant>();
    public DbSet<GrantTask> GrantTasks => Set<GrantTask>();
}
