using GrantTracker.Api.Domain;
using static GrantTracker.Tests.Domain.TestData;

namespace GrantTracker.Tests.Domain;

/// <summary>
/// BR-4: completing a task records the date; completing it again must not change that date.
/// </summary>
public class Br4CompleteTests
{
    /// <summary>
    /// BR4-1: a pending task completed on 11 Nov gets 11 Nov as its completed date.
    /// Catches: the completed date not recorded.
    /// </summary>
    [Fact]
    public void Complete_PendingTask_RecordsToday()
    {
        var task = AddTask(OpenGrant());

        task.Complete(Today);

        Assert.Equal(Today, task.CompletedDate);
    }

    /// <summary>
    /// BR4-2: a task completed on 11 Nov and completed again on 12 Nov keeps 11 Nov, with no error.
    /// Catches: the first date overwritten, or an error on a repeat.
    /// </summary>
    [Fact]
    public void Complete_AlreadyCompleted_KeepsTheFirstDate()
    {
        var task = AddTask(OpenGrant());
        task.Complete(Today);

        task.Complete(Today.AddDays(1));

        Assert.Equal(Today, task.CompletedDate);
    }

    /// <summary>
    /// BR4-3: a task due 8 Nov and completed on 11 Nov records 11 Nov and keeps its 8 Nov due date.
    /// Catches: the due date changed to make a late task look on time.
    /// </summary>
    [Fact]
    public void Complete_LateTask_KeepsItsDueDate()
    {
        var dueDate = Today.AddDays(-3);
        var task = AddTask(OpenGrant(), dueDate: dueDate, addedOn: Today.AddDays(-10));

        task.Complete(Today);

        Assert.Equal(Today, task.CompletedDate);
        Assert.Equal(dueDate, task.DueDate);
    }

    /// <summary>
    /// BR4-4: a Low-priority task on a closed grant can still be completed.
    /// Catches: nothing yet, because a task does not know its grant; it guards D24 if that link is added later.
    /// </summary>
    [Fact]
    public void Complete_TaskOnClosedGrant_IsAllowed()
    {
        var grant = OpenGrant();
        var task = AddTask(grant, priority: Priority.Low);
        grant.Close();

        task.Complete(Today);

        Assert.Equal(Today, task.CompletedDate);
    }

    /// <summary>
    /// BR4-5: a task completed on 11 Nov and completed again with an earlier date (10 Nov) keeps 11 Nov.
    /// Catches: the first date overwritten when the new date is earlier.
    /// </summary>
    [Fact]
    public void Complete_AgainWithAnEarlierDate_KeepsTheFirstDate()
    {
        var task = AddTask(OpenGrant());
        task.Complete(Today);

        task.Complete(Today.AddDays(-1));

        Assert.Equal(Today, task.CompletedDate);
    }
}
