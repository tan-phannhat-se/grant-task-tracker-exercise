using System.Globalization;

namespace GrantTracker.Api.Domain;

/// <summary>
/// A grant and its tasks. BR-1, BR-2, BR-3 and BR-5 live here; BR-4 and BR-6 live in GrantTask.
/// </summary>
public class Grant
{
    public const int ReferenceNumberMaxLength = 9; // "GR-" + 6 digits
    public const int OrganisationNameMaxLength = 200; // Australian Business Register limit (ABR data dictionary 9.9.7)

    private readonly List<GrantTask> _tasks = new();

    public int Id { get; private set; }
    public string ReferenceNumber { get; private set; } = string.Empty; // e.g. GR-100001
    public string OrganisationName { get; private set; } = string.Empty;
    public DateOnly ApplicationDate { get; private set; }
    public GrantStatus Status { get; private set; }
    // A read-only wrapper: callers cannot cast it back to List and add tasks without the rules.
    public IReadOnlyCollection<GrantTask> Tasks => _tasks.AsReadOnly();
    public int PendingTaskCount => _tasks.Count(t => !t.IsCompleted);

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

        // BR-2: the earliest allowed due date is the later of today and the application date,
        // and the error names that date so one fix is enough.
        var earliestDueDate = today > ApplicationDate ? today : ApplicationDate;
        if (dueDate < earliestDueDate)
        {
            var code = earliestDueDate == today ? ErrorCodes.DueDateInPast : ErrorCodes.DueDateBeforeApplication;
            var earliest = earliestDueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            throw new DomainRuleException(
                code,
                DomainErrorKind.Validation,
                $"Due date must be on or after {earliest}.",
                field: "dueDate");
        }

        // A priority outside Low, Medium and High would never count as High, so it could slip past BR-5.
        if (!Enum.IsDefined(priority))
        {
            throw new DomainRuleException(
                ErrorCodes.PriorityInvalid,
                DomainErrorKind.Validation,
                "Priority must be Low, Medium or High.",
                field: "priority");
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
        // BR-5, refuse while a High-priority task is pending.
        var pendingHighTasks = _tasks
            .Where(t => !t.IsCompleted)
            .Where(t => t.Priority == Priority.High)
            .ToList();

        if (pendingHighTasks.Any())
        {
            var taskTitles = string.Join(", ", pendingHighTasks.Select(t => t.Title));
            throw new DomainRuleException(
                ErrorCodes.PendingHighPriorityTasks,
                DomainErrorKind.RuleRefused,
                "Cannot close a grant with a High-priority task pending: " + taskTitles);
        }

        Status = GrantStatus.Closed; // closing a closed grant again changes nothing
    }
}
