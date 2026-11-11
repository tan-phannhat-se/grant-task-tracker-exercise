using GrantTracker.Api.Domain;

namespace GrantTracker.Api.Data;

/// <summary>
/// The sample grants and tasks from the brief, with dates relative to "today".
/// </summary>
public static class SeedData
{
    /// <summary>
    /// Adds the sample grants and their tasks, then saves.
    /// </summary>
    public static void Seed(GrantTrackerDbContext db, DateOnly today)
    {
        // Built through the domain methods, so the sample data obeys every business rule.
        var riverside = new Grant("GR-100001", "Riverside Community Garden Inc.", today.AddDays(-60));
        AddTaskOnApplicationDate(riverside, "Verify bank details", Priority.High, today.AddDays(-3));
        AddTaskOnApplicationDate(riverside, "Request quote for tools", Priority.Medium, today.AddDays(5));
        AddTaskCompletedOnDueDate(riverside, "Check eligibility documents", Priority.High, today.AddDays(-50));

        var northside = new Grant("GR-100002", "Northside Youth Sports Club", today.AddDays(-20));
        AddTaskOnApplicationDate(northside, "Review progress report", Priority.Low, today.AddDays(-1));
        AddTaskOnApplicationDate(northside, "Schedule site visit", Priority.Medium, today.AddDays(10));

        var harbour = new Grant("GR-100003", "Harbour Arts Collective", today.AddDays(-200));
        AddTaskCompletedOnDueDate(harbour, "Final acquittal review", Priority.High, today.AddDays(-150));
        harbour.Close(); // after its High task is completed, so BR-5 allows it

        var hillview = new Grant("GR-100004", "Hillview Library Friends Group", today.AddDays(-5));

        db.Grants.AddRange(riverside, northside, harbour, hillview);
        db.SaveChanges();
    }

    /// <summary>
    /// Adds a task as an officer would have on the grant's application date, so BR-2 accepts past due dates.
    /// </summary>
    private static GrantTask AddTaskOnApplicationDate(Grant grant, string title, Priority priority, DateOnly dueDate)
    {
        var addedOn = grant.ApplicationDate;
        var createdAtUtc = addedOn.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        return grant.AddTask(title, dueDate, priority, addedOn, createdAtUtc);
    }

    /// <summary>
    /// Adds a task and completes it on its due date; the brief gives no completion dates.
    /// </summary>
    private static void AddTaskCompletedOnDueDate(Grant grant, string title, Priority priority, DateOnly dueDate)
    {
        var task = AddTaskOnApplicationDate(grant, title, priority, dueDate);
        task.Complete(dueDate);
    }
}
