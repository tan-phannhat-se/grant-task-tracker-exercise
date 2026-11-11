using GrantTracker.Api.Endpoints.Responses;
using GrantTracker.Api.Services;

namespace GrantTracker.Api.Endpoints;

/// <summary>
/// The /tasks handlers: HTTP in and out only; the work is done by GrantTaskService.
/// </summary>
public static partial class TaskEndpoints
{
    private static async Task<TaskResponse> CompleteTask(
        int id,
        GrantTaskService grantTaskService,
        ConfiguredTimeZoneCalendar calendar)
    {
        var task = await grantTaskService.CompleteTaskAsync(id);
        return TaskResponse.From(task, calendar.Today());
    }
}
