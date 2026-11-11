namespace GrantTracker.Api.Domain;

/// <summary>
/// A piece of work on a grant. Created only through <see cref="Grant.AddTask"/>.
/// </summary>
public class GrantTask
{
    public const int TitleMaxLength = 100; // BR-3

    public int Id { get; private set; }
    public int GrantId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public DateOnly DueDate { get; private set; }
    public Priority Priority { get; private set; }

    /// <summary>
    /// The day the task was completed, or null while it is pending.
    /// One field instead of a flag plus a date, so the two can never disagree.
    /// Completing again keeps this first date.
    /// No status field: pending and overdue are derived; one is only needed for user-chosen states (e.g. Cancelled).
    /// </summary>
    public DateOnly? CompletedDate { get; private set; }

    public DateTime CreatedAt { get; private set; } // UTC

    /// <summary>
    /// The key the client sent with the "add task" request, or null if none was sent.
    /// A retry with the same key returns this task instead of creating a duplicate.
    /// Stored on the task row so the task and its key are saved in one transaction;
    /// a separate store such as Redis could not join that transaction.
    /// </summary>
    public string? IdempotencyKey { get; private set; }

    public bool IsCompleted => CompletedDate is not null;

    private GrantTask() { } // for EF Core

    internal GrantTask(
        string title,
        DateOnly dueDate,
        Priority priority,
        DateTime createdAtUtc,
        string? idempotencyKey)
    {
        Title = title;
        DueDate = dueDate;
        Priority = priority;
        CreatedAt = createdAtUtc;
        IdempotencyKey = idempotencyKey;
    }

    /// <summary>
    /// Marks the task completed on <paramref name="today"/>. A repeat keeps the first date.
    /// </summary>
    public void Complete(DateOnly today)
    {
        throw new NotImplementedException("T3.5");
    }

    /// <summary>
    /// True when the task is not completed and was due before <paramref name="today"/>.
    /// </summary>
    public bool IsOverdue(DateOnly today)
    {
        throw new NotImplementedException("T3.7");
    }
}
