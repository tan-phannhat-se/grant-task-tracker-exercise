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

    /// <summary>
    /// Loads one grant with its tasks, or reports grant-not-found.
    /// </summary>
    public async Task<Grant> GetGrantAsync(int id)
    {
        var grant = await _db.Grants
            .Include(g => g.Tasks)
            .AsNoTracking()
            .SingleOrDefaultAsync(g => g.Id == id);

        return grant ?? throw new DomainRuleException(
            ErrorCodes.GrantNotFound,
            DomainErrorKind.NotFound,
            $"Grant {id} was not found.");
    }
}
