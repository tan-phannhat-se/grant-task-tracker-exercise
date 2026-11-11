namespace GrantTracker.Api.Domain;

/// <summary>
/// Every error code the domain can raise, shared by the API, the tests and the screen.
/// </summary>
public static class ErrorCodes
{
    public const string GrantClosed = "grant-closed"; // BR-1
    public const string DueDateInPast = "due-date-in-past"; // BR-2
    public const string DueDateBeforeApplication = "due-date-before-application"; // BR-2
    public const string DueDateRequired = "due-date-required"; // the JSON had no dueDate
    public const string TitleRequired = "title-required"; // BR-3
    public const string TitleTooLong = "title-too-long"; // BR-3
    public const string PendingHighPriorityTasks = "pending-high-priority-tasks"; // BR-5
    public const string PriorityInvalid = "priority-invalid"; // only Low, Medium or High
    public const string GrantNotFound = "grant-not-found";
    public const string StatusInvalid = "status-invalid"; // grant list filter: Open, Closed or All
}
