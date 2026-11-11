using GrantTracker.Api.Domain;
using static GrantTracker.Tests.Domain.TestData;

namespace GrantTracker.Tests.Domain;

/// <summary>
/// BR-2: a new task's due date cannot be in the past, and cannot be before the grant's application date.
/// </summary>
public class Br2DueDateTests
{
    private static readonly DateOnly Yesterday = Today.AddDays(-1);
    private static readonly DateOnly FutureApplicationDate = new(2026, 11, 20);

    /// <summary>
    /// BR2-1: a grant that applied in the past refuses a task due yesterday with due-date-in-past on dueDate.
    /// Catches: condition 1 (not in the past) missing.
    /// </summary>
    [Fact]
    public void AddTask_DueYesterday_IsRefusedAsInThePast()
    {
        var grant = OpenGrant();

        var error = Assert.Throws<DomainRuleException>(() => AddTask(grant, dueDate: Yesterday));

        Assert.Equal(ErrorCodes.DueDateInPast, error.Code);
        Assert.Equal(DomainErrorKind.Validation, error.Kind);
        Assert.Equal("dueDate", error.Field);
        Assert.Contains("2026-11-11", error.Message);
        Assert.Empty(grant.Tasks);
    }

    /// <summary>
    /// BR2-2: a grant that applied in the past accepts a task due today.
    /// Catches: `<=` instead of `<` at today.
    /// </summary>
    [Fact]
    public void AddTask_DueToday_IsAccepted()
    {
        var task = AddTask(OpenGrant(), dueDate: Today);

        Assert.Equal(Today, task.DueDate);
    }

    /// <summary>
    /// BR2-3: a grant applying on 20 Nov refuses a task due 15 Nov with due-date-before-application.
    /// Catches: condition 2 (not before the application date) missing.
    /// </summary>
    [Fact]
    public void AddTask_DueBeforeApplicationDate_IsRefused()
    {
        var grant = OpenGrant(applicationDate: FutureApplicationDate);

        var error = Assert.Throws<DomainRuleException>(() => AddTask(grant, dueDate: new DateOnly(2026, 11, 15)));

        Assert.Equal(ErrorCodes.DueDateBeforeApplication, error.Code);
        Assert.Equal(DomainErrorKind.Validation, error.Kind);
        Assert.Equal("dueDate", error.Field);
    }

    /// <summary>
    /// BR2-4: a grant applying on 20 Nov accepts a task due 20 Nov.
    /// Catches: `<=` instead of `<` at the application date.
    /// </summary>
    [Fact]
    public void AddTask_DueOnApplicationDate_IsAccepted()
    {
        var grant = OpenGrant(applicationDate: FutureApplicationDate);

        var task = AddTask(grant, dueDate: FutureApplicationDate);

        Assert.Equal(FutureApplicationDate, task.DueDate);
    }

    /// <summary>
    /// BR2-5: a grant applying on 20 Nov refuses a task due yesterday with due-date-before-application,
    /// and the message names 20 Nov, the earliest date that would work.
    /// Catches: the error reporting "in the past" when the application date is the real limit.
    /// </summary>
    [Fact]
    public void AddTask_DueYesterdayOnFutureApplication_ReportsTheApplicationDate()
    {
        var grant = OpenGrant(applicationDate: FutureApplicationDate);

        var error = Assert.Throws<DomainRuleException>(() => AddTask(grant, dueDate: Yesterday));

        Assert.Equal(ErrorCodes.DueDateBeforeApplication, error.Code);
        Assert.Equal("dueDate", error.Field);
        Assert.Contains("2026-11-20", error.Message);
    }

    /// <summary>
    /// BR2-6: a grant applying today refuses a task due yesterday with due-date-in-past.
    /// Catches: the wrong code when today and the application date are the same day.
    /// </summary>
    [Fact]
    public void AddTask_ApplicationTodayDueYesterday_IsRefusedAsInThePast()
    {
        var grant = OpenGrant(applicationDate: Today);

        var error = Assert.Throws<DomainRuleException>(() => AddTask(grant, dueDate: Yesterday));

        Assert.Equal(ErrorCodes.DueDateInPast, error.Code);
    }
}
