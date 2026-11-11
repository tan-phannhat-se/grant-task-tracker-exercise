using GrantTracker.Api.Domain;

namespace GrantTracker.Api.Endpoints.Responses;

/// <summary>
/// One row of the overdue list: the task, its grant and how many days it is overdue.
/// </summary>
public record OverdueTaskResponse(
    int Id,
    int GrantId,
    string Title,
    DateOnly DueDate,
    Priority Priority,
    DateOnly? CompletedDate,
    bool IsOverdue,
    bool CompletedLate,
    string ReferenceNumber,
    string OrganisationName,
    int DaysOverdue)
{
    /// <summary>
    /// Builds the row from an overdue task and its grant.
    /// </summary>
    public static OverdueTaskResponse From(Grant grant, GrantTask task, DateOnly today) => new(
        task.Id,
        task.GrantId,
        task.Title,
        task.DueDate,
        task.Priority,
        task.CompletedDate,
        task.IsOverdue(today),
        task.IsCompletedLate,
        grant.ReferenceNumber,
        grant.OrganisationName,
        today.DayNumber - task.DueDate.DayNumber);
}
