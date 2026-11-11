using GrantTracker.Api.Domain;
using static GrantTracker.Tests.Domain.TestData;

namespace GrantTracker.Tests.Domain;

/// <summary>
/// BR-5: a grant cannot be closed while it has any pending High-priority task.
/// </summary>
public class Br5CloseGrantTests
{
    /// <summary>
    /// BR5-1: one pending High task blocks closing with pending-high-priority-tasks (409);
    /// the grant stays Open and the message names the task.
    /// Catches: BR-5 missing, or the grant closed even though the error was raised.
    /// </summary>
    [Fact]
    public void Close_OnePendingHighTask_IsRefused()
    {
        var grant = OpenGrant();
        AddTask(grant, Priority.High, title: "Verify bank details");

        var error = Assert.Throws<DomainRuleException>(grant.Close);

        Assert.Equal(ErrorCodes.PendingHighPriorityTasks, error.Code);
        Assert.Equal(DomainErrorKind.RuleRefused, error.Kind);
        Assert.Equal(GrantStatus.Open, grant.Status);
        Assert.Contains("Verify bank details", error.Message);
    }

    /// <summary>
    /// BR5-2: two pending High tasks block closing, and the message names both.
    /// Catches: only the first blocking task listed.
    /// </summary>
    [Fact]
    public void Close_TwoPendingHighTasks_NamesBoth()
    {
        var grant = OpenGrant();
        AddTask(grant, Priority.High, title: "Verify bank details");
        AddTask(grant, Priority.High, title: "Check eligibility documents");

        var error = Assert.Throws<DomainRuleException>(grant.Close);

        Assert.Contains("Verify bank details", error.Message);
        Assert.Contains("Check eligibility documents", error.Message);
    }

    /// <summary>
    /// BR5-3: a grant whose only pending tasks are Low and Medium can be closed.
    /// Catches: any pending task blocking the close (too strict).
    /// </summary>
    [Fact]
    public void Close_OnlyLowAndMediumPending_Closes()
    {
        var grant = OpenGrant();
        AddTask(grant, Priority.Low);
        AddTask(grant, Priority.Medium);

        grant.Close();

        Assert.Equal(GrantStatus.Closed, grant.Status);
    }

    /// <summary>
    /// BR5-4: a completed High task with a pending Low task does not block closing.
    /// Catches: the priority checked without checking whether the task is completed.
    /// </summary>
    [Fact]
    public void Close_HighTaskCompletedAndLowPending_Closes()
    {
        var grant = OpenGrant();
        AddTask(grant, Priority.High).Complete(Today);
        AddTask(grant, Priority.Low);

        grant.Close();

        Assert.Equal(GrantStatus.Closed, grant.Status);
    }

    /// <summary>
    /// BR5-5: a grant with no tasks can be closed.
    /// Catches: a crash or a refusal on an empty task list.
    /// </summary>
    [Fact]
    public void Close_NoTasks_Closes()
    {
        var grant = OpenGrant();

        grant.Close();

        Assert.Equal(GrantStatus.Closed, grant.Status);
    }

    /// <summary>
    /// BR5-6: closing an already closed grant raises no error and it stays Closed.
    /// Catches: an error on a repeat.
    /// </summary>
    [Fact]
    public void Close_AlreadyClosed_StaysClosedWithoutError()
    {
        var grant = OpenGrant();
        grant.Close();

        grant.Close();

        Assert.Equal(GrantStatus.Closed, grant.Status);
    }
}
