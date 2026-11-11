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

    private static DomainRuleException TaskNotFound(int id) => new(
        ErrorCodes.TaskNotFound,
        DomainErrorKind.NotFound,
        $"Task {id} was not found.");
}
