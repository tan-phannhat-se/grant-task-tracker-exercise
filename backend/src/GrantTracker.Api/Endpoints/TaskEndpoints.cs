using GrantTracker.Api.Domain;
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

    private static async Task<IReadOnlyList<OverdueTaskResponse>> ListOverdueTasks(
        GrantTaskService grantTaskService,
        ConfiguredTimeZoneCalendar calendar,
        string? grantStatus)
    {
        // Missing means Open: the default list shows only work officers can still act on.
        var status = GrantStatusFilter.Parse(grantStatus, whenMissing: GrantStatus.Open, field: "grantStatus");
        var rows = await grantTaskService.ListOverdueTasksAsync(status);

        var today = calendar.Today();
        return rows.Select(row => OverdueTaskResponse.From(row.Grant, row.Task, today)).ToList();
    }
}
