namespace GrantTracker.Api.Domain;

/// <summary>
/// A grant and its tasks. Every rule lives here and takes "today" as input, so it can be tested.
/// </summary>
public class Grant
{
    private readonly List<GrantTask> _tasks = new();

    public int Id { get; private set; }
    public string ReferenceNumber { get; private set; } = string.Empty; // e.g. GR-100001
    public string OrganisationName { get; private set; } = string.Empty;
    public DateOnly ApplicationDate { get; private set; }
    public GrantStatus Status { get; private set; }
    public IReadOnlyCollection<GrantTask> Tasks => _tasks;

    private Grant() { } // for EF Core

    /// <summary>
    /// Opens a new grant.
    /// </summary>
    public Grant(string referenceNumber, string organisationName, DateOnly applicationDate)
    {
        ReferenceNumber = referenceNumber;
        OrganisationName = organisationName;
        ApplicationDate = applicationDate;
        Status = GrantStatus.Open;
    }

    /// <summary>
    /// Adds a task after checking BR-1, BR-3 and BR-2.
    /// </summary>
    public GrantTask AddTask(
        string title,
        DateOnly dueDate,
        Priority priority,
        DateOnly today,
        DateTime createdAtUtc,
        string? idempotencyKey = null)
    {
        throw new NotImplementedException("T3.2–T3.4");
    }

    /// <summary>
    /// Closes the grant unless a High-priority task is pending (BR-5).
    /// </summary>
    public void Close()
    {
        throw new NotImplementedException("T3.6, written by tan");
    }
}
