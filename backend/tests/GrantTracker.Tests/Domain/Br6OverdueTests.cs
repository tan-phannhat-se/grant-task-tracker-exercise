using GrantTracker.Api.Domain;
using static GrantTracker.Tests.Domain.TestData;

namespace GrantTracker.Tests.Domain;

/// <summary>
/// BR-6: a task is overdue when it is not completed and its due date is before today.
/// Also covers "completed late" (D26), which is a different idea from overdue.
/// </summary>
public class Br6OverdueTests
{
    private static readonly DateOnly TenthOfNovember = new(2026, 11, 10);

    /// <summary>
    /// BR6-1: a pending task due yesterday is overdue.
    /// Catches: BR-6 missing.
    /// </summary>
    [Fact]
    public void IsOverdue_PendingDueYesterday_IsTrue()
    {
        var task = AddTask(OpenGrant(), dueDate: Today.AddDays(-1), addedOn: Today.AddDays(-5));

        Assert.True(task.IsOverdue(Today));
    }

    /// <summary>
    /// BR6-2: a pending task due today is not overdue.
    /// Catches: `<=` instead of `<` at today.
    /// </summary>
    [Fact]
    public void IsOverdue_PendingDueToday_IsFalse()
    {
        var task = AddTask(OpenGrant(), dueDate: Today);

        Assert.False(task.IsOverdue(Today));
    }

    /// <summary>
    /// BR6-3: a task due 10 Nov and completed on 7 Nov or on 11 Nov is not overdue on 11 Nov.
    /// Catches: completion not checked, or "completed late" mistaken for overdue.
    /// </summary>
    [Theory]
    [InlineData(7)]
    [InlineData(11)]
    public void IsOverdue_CompletedTask_IsFalse(int completedOnDay)
    {
        var task = AddTask(OpenGrant(), dueDate: TenthOfNovember, addedOn: new DateOnly(2026, 11, 5));
        task.Complete(new DateOnly(2026, 11, completedOnDay));

        Assert.False(task.IsOverdue(Today));
    }

    /// <summary>
    /// BR6-4: the same pending task due 12 Nov is not overdue on 11 Nov but is overdue on 13 Nov.
    /// Catches: the real clock or a stored state used instead of the given "today".
    /// </summary>
    [Fact]
    public void IsOverdue_DependsOnTheGivenToday()
    {
        var task = AddTask(OpenGrant(), dueDate: new DateOnly(2026, 11, 12));

        Assert.False(task.IsOverdue(new DateOnly(2026, 11, 11)));
        Assert.True(task.IsOverdue(new DateOnly(2026, 11, 13)));
    }

    /// <summary>
    /// BR6-5: a task due 10 Nov and completed on 11 Nov is completed late, and is not overdue.
    /// Catches: "completed late" and "overdue" mixed up (D26).
    /// </summary>
    [Fact]
    public void IsCompletedLate_CompletedAfterDueDate_IsTrueAndNotOverdue()
    {
        var task = AddTask(OpenGrant(), dueDate: TenthOfNovember, addedOn: new DateOnly(2026, 11, 5));
        task.Complete(Today);

        Assert.True(task.IsCompletedLate);
        Assert.False(task.IsOverdue(Today));
    }

    /// <summary>
    /// BR6-6: a task due 10 Nov and completed on 10 Nov is not completed late.
    /// Catches: `>=` instead of `>` (completing on the due date is on time).
    /// </summary>
    [Fact]
    public void IsCompletedLate_CompletedOnDueDate_IsFalse()
    {
        var task = AddTask(OpenGrant(), dueDate: TenthOfNovember, addedOn: new DateOnly(2026, 11, 5));
        task.Complete(TenthOfNovember);

        Assert.False(task.IsCompletedLate);
    }

    /// <summary>
    /// BR6-7: a task due 10 Nov and completed on 7 Nov is not completed late.
    /// Catches: "completed late" true for any completion that is not on the due date.
    /// </summary>
    [Fact]
    public void IsCompletedLate_CompletedBeforeDueDate_IsFalse()
    {
        var task = AddTask(OpenGrant(), dueDate: TenthOfNovember, addedOn: new DateOnly(2026, 11, 5));
        task.Complete(new DateOnly(2026, 11, 7));

        Assert.False(task.IsCompletedLate);
    }

    /// <summary>
    /// BR6-8: a pending task due yesterday is overdue but not completed late.
    /// Catches: a pending task flagged as completed late.
    /// </summary>
    [Fact]
    public void IsCompletedLate_PendingPastDue_IsFalseAndOverdue()
    {
        var task = AddTask(OpenGrant(), dueDate: Today.AddDays(-1), addedOn: Today.AddDays(-5));

        Assert.False(task.IsCompletedLate);
        Assert.True(task.IsOverdue(Today));
    }
}
