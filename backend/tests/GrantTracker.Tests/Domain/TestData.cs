using GrantTracker.Api.Domain;

namespace GrantTracker.Tests.Domain;

/// <summary>
/// Fixed dates and builders for the domain tests. No test uses the real date.
/// </summary>
public static class TestData
{
    public static readonly DateOnly Today = new(2026, 11, 11);
    public static readonly DateTime NowUtc = new(2026, 11, 11, 9, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// An open grant that applied 30 days before <see cref="Today"/>, unless told otherwise.
    /// </summary>
    public static Grant OpenGrant(DateOnly? applicationDate = null) =>
        new("GR-TEST-1", "Test Organisation", applicationDate ?? Today.AddDays(-30));

    /// <summary>
    /// Adds a valid task. <paramref name="addedOn"/> lets a test create an already overdue task.
    /// </summary>
    public static GrantTask AddTask(
        Grant grant,
        Priority priority = Priority.Medium,
        DateOnly? dueDate = null,
        string title = "Test task",
        DateOnly? addedOn = null)
    {
        var day = addedOn ?? Today;
        return grant.AddTask(title, dueDate ?? day.AddDays(7), priority, day, NowUtc);
    }
}
