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
        // BR-1 first: when the grant is closed, fixing the input cannot help.
        if (Status == GrantStatus.Closed)
        {
            throw new DomainRuleException(
                ErrorCodes.GrantClosed,
                DomainErrorKind.RuleRefused,
                $"Grant {ReferenceNumber} is closed, so no task can be added.");
        }

        // BR-3: check and save the trimmed title, so what is checked is what is saved.
        var trimmedTitle = title.Trim();
        switch (trimmedTitle.Length)
        {
            case 0:
                throw new DomainRuleException(
                    ErrorCodes.TitleRequired,
                    DomainErrorKind.Validation,
                    "Title is required.",
                    field: "title");

            case > GrantTask.TitleMaxLength:
                throw new DomainRuleException(
                    ErrorCodes.TitleTooLong,
                    DomainErrorKind.Validation,
                    $"Title must be {GrantTask.TitleMaxLength} characters or fewer (it has {trimmedTitle.Length}).",
                    field: "title");
        }

        var task = new GrantTask(trimmedTitle, dueDate, priority, createdAtUtc, idempotencyKey);
        _tasks.Add(task);
        return task;
    }

    /// <summary>
    /// Closes the grant unless a High-priority task is pending (BR-5).
    /// </summary>
    public void Close()
    {
        // TODO T3.5 (tan): BR-5, refuse while a High-priority task is pending.
        Status = GrantStatus.Closed; // closing a closed grant again changes nothing
    }
}
