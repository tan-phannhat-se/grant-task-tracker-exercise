using GrantTracker.Api.Data;
using GrantTracker.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace GrantTracker.Api.Services;

/// <summary>
/// Use cases for tasks: load them from the database, call the domain, save.
/// </summary>
public class GrantTaskService
{
    private readonly GrantTrackerDbContext _db;
    private readonly ConfiguredTimeZoneCalendar _calendar;

    /// <summary>
    /// Creates the service for one request's database session and the app's calendar.
    /// </summary>
    public GrantTaskService(GrantTrackerDbContext db, ConfiguredTimeZoneCalendar calendar)
    {
        _db = db;
        _calendar = calendar;
    }

    /// <summary>
    /// Completes a task today; a task already completed keeps its first date.
    /// </summary>
    public async Task<GrantTask> CompleteTaskAsync(int id)
    {
        var task = await _db.GrantTasks.SingleOrDefaultAsync(t => t.Id == id) ?? throw TaskNotFound(id);

        task.Complete(_calendar.Today());
        await _db.SaveChangesAsync();
        return task;
    }

    /// <summary>
    /// Lists the overdue tasks of grants with the given status (every grant when null), most overdue first.
    /// </summary>
    public async Task<IReadOnlyList<(Grant Grant, GrantTask Task)>> ListOverdueTasksAsync(GrantStatus? grantStatus)
    {
        // Grants are loaded with all their tasks so the domain decides what is overdue (BR-6 stays in one place).
        var grants = _db.Grants.Include(g => g.Tasks).AsNoTracking();
        if (grantStatus is not null)
        {
            grants = grants.Where(g => g.Status == grantStatus);
        }

        var today = _calendar.Today();
        return (await grants.ToListAsync())
            .SelectMany(g => g.Tasks.Where(t => t.IsOverdue(today)).Select(t => (Grant: g, Task: t)))
            .OrderBy(row => row.Task.DueDate)
            .ThenBy(row => row.Task.Id)
            .ToList();
    }

    private static DomainRuleException TaskNotFound(int id) => new(
        ErrorCodes.TaskNotFound,
        DomainErrorKind.NotFound,
        $"Task {id} was not found.");
}
