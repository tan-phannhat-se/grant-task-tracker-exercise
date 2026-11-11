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
    private readonly ConfiguredTimeZoneCalendar _calendar;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Creates the service for one request's database session and the app's clock.
    /// </summary>
    public GrantService(GrantTrackerDbContext db, ConfiguredTimeZoneCalendar calendar, TimeProvider timeProvider)
    {
        _db = db;
        _calendar = calendar;
        _timeProvider = timeProvider;
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

        return grant ?? throw GrantNotFound(id);
    }

    /// <summary>
    /// Adds a task to a grant through the domain rules and saves it, or returns the task an earlier request
    /// with the same idempotency key added.
    /// </summary>
    public async Task<AddTaskResult> AddTaskAsync(
        int grantId,
        string title,
        DateOnly dueDate,
        Priority priority,
        string? idempotencyKey)
    {
        // The key is looked up before the grant and its rules: the earlier request worked,
        // so its retry must too, even if the grant has been closed since.
        if (idempotencyKey is not null)
        {
            var earlierTask = await _db.GrantTasks.SingleOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey);
            if (earlierTask is not null)
            {
                return earlierTask.MatchesRequest(grantId, title, dueDate, priority)
                    ? new AddTaskResult(earlierTask, IsRetry: true)
                    : throw KeyReused();
            }
        }

        // Tasks are not loaded: adding one needs only the grant's status and application date.
        var grant = await _db.Grants.SingleOrDefaultAsync(g => g.Id == grantId) ?? throw GrantNotFound(grantId);

        var task = grant.AddTask(
            title, dueDate, priority, _calendar.Today(), _timeProvider.GetUtcNow().UtcDateTime, idempotencyKey);
        await _db.SaveChangesAsync();
        return new AddTaskResult(task, IsRetry: false);
    }

    /// <summary>
    /// Closes a grant unless a High-priority task is pending; closing a closed grant changes nothing.
    /// </summary>
    public async Task<Grant> CloseGrantAsync(int id)
    {
        // Tasks must be loaded: without them BR-5 would see no pending High task and let the close through.
        var grant = await _db.Grants
            .Include(g => g.Tasks)
            .SingleOrDefaultAsync(g => g.Id == id) ?? throw GrantNotFound(id);

        grant.Close();
        await _db.SaveChangesAsync();
        return grant;
    }

    private static DomainRuleException GrantNotFound(int id) => new(
        ErrorCodes.GrantNotFound,
        DomainErrorKind.NotFound,
        $"Grant {id} was not found.");

    // The message never describes the earlier task: it may belong to another grant.
    private static DomainRuleException KeyReused() => new(
        ErrorCodes.IdempotencyKeyReused,
        DomainErrorKind.RequestMismatch,
        "This Idempotency-Key was already used for a different request. Send a new key.");
}
