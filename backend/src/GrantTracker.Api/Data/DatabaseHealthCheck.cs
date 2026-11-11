using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GrantTracker.Api.Data;

/// <summary>
/// Health check for the database.
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly GrantTrackerDbContext _db;

    /// <summary>
    /// Creates the health check.
    /// </summary>
    public DatabaseHealthCheck(GrantTrackerDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Healthy when the data can be read, Unhealthy otherwise.
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Reads a row, not just opens a connection: with the tables gone, opening still succeeds.
            await _db.Grants.AnyAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("The database cannot be read.", ex);
        }
    }
}
