using GrantTracker.Api.Domain;
using static GrantTracker.Tests.Domain.TestData;

namespace GrantTracker.Tests.Domain;

/// <summary>
/// A priority that is not Low, Medium or High is refused, so it cannot slip past BR-5.
/// </summary>
public class PriorityTests
{
    /// <summary>
    /// PR-1: a priority of 0, 150 or 999 is refused with priority-invalid on the priority field, and no task is added.
    /// Catches: an undefined priority accepted (BR-5 would never count it as High),
    /// or a range check that lets 150 through.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(150)]
    [InlineData(999)]
    public void AddTask_UndefinedPriority_IsRefused(int value)
    {
        var grant = OpenGrant();

        var error = Assert.Throws<DomainRuleException>(() => AddTask(grant, priority: (Priority)value));

        Assert.Equal(ErrorCodes.PriorityInvalid, error.Code);
        Assert.Equal(DomainErrorKind.Validation, error.Kind);
        Assert.Equal("priority", error.Field);
        Assert.Empty(grant.Tasks);
    }
}
