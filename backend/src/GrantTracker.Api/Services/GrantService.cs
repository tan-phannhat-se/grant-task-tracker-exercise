using GrantTracker.Api.Data;
using GrantTracker.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace GrantTracker.Api.Services;

/// <summary>
/// Use cases for grants: load them from the database, call the domain, save.
/// </summary>
public class GrantService
{
    private readonly GrantTrackerDbContext _db;

    /// <summary>
    /// Creates the service for one request's database session.
    /// </summary>
    public GrantService(GrantTrackerDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Lists grants by reference number, only those with the given status when one is given.
    /// </summary>
    public async Task<IReadOnlyList<Grant>> ListGrantsAsync(GrantStatus? status)
    {
        // Tasks are loaded so the domain counts the pending ones.
        var grants = _db.Grants.Include(g => g.Tasks).AsNoTracking();
        if (status is not null)
        {
            grants = grants.Where(g => g.Status == status);
        }

        return await grants.OrderBy(g => g.ReferenceNumber).ToListAsync();
    }
}
