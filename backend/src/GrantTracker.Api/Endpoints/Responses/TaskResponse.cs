using GrantTracker.Api.Domain;

namespace GrantTracker.Api.Endpoints.Responses;

/// <summary>
/// A task as the screen shows it, with the overdue and completed-late flags worked out by the server.
/// </summary>
public record TaskResponse(
    int Id,
    int GrantId,
    string Title,
    DateOnly DueDate,
    Priority Priority,
    DateOnly? CompletedDate,
    bool IsOverdue,
    bool CompletedLate)
{
    /// <summary>
    /// Builds the response; "today" decides whether the task is overdue.
    /// </summary>
    public static TaskResponse From(GrantTask task, DateOnly today) => new(
        task.Id,
        task.GrantId,
        task.Title,
        task.DueDate,
        task.Priority,
        task.CompletedDate,
        task.IsOverdue(today),
        task.IsCompletedLate);
}
