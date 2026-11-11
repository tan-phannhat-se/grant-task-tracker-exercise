using GrantTracker.Api.Domain;
using static GrantTracker.Tests.Domain.TestData;

namespace GrantTracker.Tests.Domain;

/// <summary>
/// BR-1: a task cannot be added to a closed grant.
/// </summary>
public class Br1ClosedGrantTests
{
    /// <summary>
    /// BR1-1: an open grant accepts a valid task.
    /// Catches: the rule blocking open grants too.
    /// </summary>
    [Fact]
    public void AddTask_OpenGrant_AddsTheTask()
    {
        var grant = OpenGrant();

        var task = AddTask(grant);

        Assert.Contains(task, grant.Tasks);
    }

    /// <summary>
    /// BR1-2: a closed grant refuses a valid task with grant-closed (409) and its task list stays empty.
    /// Catches: BR-1 missing, or the task added even though the error was raised.
    /// </summary>
    [Fact]
    public void AddTask_ClosedGrant_IsRefusedAndAddsNothing()
    {
        var grant = OpenGrant();
        grant.Close();

        var error = Assert.Throws<DomainRuleException>(() => AddTask(grant));

        Assert.Equal(ErrorCodes.GrantClosed, error.Code);
        Assert.Equal(DomainErrorKind.RuleRefused, error.Kind);
        Assert.Empty(grant.Tasks);
    }

    /// <summary>
    /// BR1-3: a closed grant with an empty title reports grant-closed, not title-required.
    /// Catches: the checks running in the wrong order (BR-1 comes first).
    /// </summary>
    [Fact]
    public void AddTask_ClosedGrantAndEmptyTitle_ReportsTheClosedGrantFirst()
    {
        var grant = OpenGrant();
        grant.Close();

        var error = Assert.Throws<DomainRuleException>(() => AddTask(grant, title: ""));

        Assert.Equal(ErrorCodes.GrantClosed, error.Code);
    }

    /// <summary>
    /// BR1-4: a closed grant with a past due date reports grant-closed, not due-date-in-past.
    /// Catches: the due-date check running before BR-1.
    /// </summary>
    [Fact]
    public void AddTask_ClosedGrantAndPastDueDate_ReportsTheClosedGrantFirst()
    {
        var grant = OpenGrant();
        grant.Close();

        var error = Assert.Throws<DomainRuleException>(() => AddTask(grant, dueDate: Today.AddDays(-1)));

        Assert.Equal(ErrorCodes.GrantClosed, error.Code);
    }
}
