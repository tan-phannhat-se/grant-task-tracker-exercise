using GrantTracker.Api.Domain;
using static GrantTracker.Tests.Domain.TestData;

namespace GrantTracker.Tests.Domain;

/// <summary>
/// BR-3: the task title is required and must be 100 characters or fewer, measured after trimming.
/// </summary>
public class Br3TitleTests
{
    /// <summary>
    /// BR3-1: an empty title, or one with only spaces, is refused with title-required on the title field.
    /// Catches: the empty check missing, or the title not trimmed before checking.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddTask_TitleMissing_IsRefused(string title)
    {
        var grant = OpenGrant();

        var error = Assert.Throws<DomainRuleException>(() => AddTask(grant, title: title));

        Assert.Equal(ErrorCodes.TitleRequired, error.Code);
        Assert.Equal(DomainErrorKind.Validation, error.Kind);
        Assert.Equal("title", error.Field);
        Assert.Empty(grant.Tasks);
    }

    /// <summary>
    /// BR3-2: a title of exactly 100 characters is accepted.
    /// Catches: `>=` instead of `>` at the limit.
    /// </summary>
    [Fact]
    public void AddTask_TitleOfExactly100Characters_IsAccepted()
    {
        var title = new string('a', 100);

        var task = AddTask(OpenGrant(), title: title);

        Assert.Equal(title, task.Title);
    }

    /// <summary>
    /// BR3-3: a title of 101 characters is refused with title-too-long on the title field.
    /// Catches: the length check missing.
    /// </summary>
    [Fact]
    public void AddTask_TitleOf101Characters_IsRefused()
    {
        var grant = OpenGrant();

        var error = Assert.Throws<DomainRuleException>(() => AddTask(grant, title: new string('a', 101)));

        Assert.Equal(ErrorCodes.TitleTooLong, error.Code);
        Assert.Equal(DomainErrorKind.Validation, error.Kind);
        Assert.Equal("title", error.Field);
    }

    /// <summary>
    /// BR3-4: spaces at both ends are removed and the trimmed title is saved.
    /// Catches: the title checked after trimming but saved untrimmed.
    /// </summary>
    [Fact]
    public void AddTask_TitleWithSurroundingSpaces_IsSavedTrimmed()
    {
        var task = AddTask(OpenGrant(), title: "  Check docs  ");

        Assert.Equal("Check docs", task.Title);
    }

    /// <summary>
    /// BR3-5: 100 characters with two spaces on each side (104 in total) are accepted.
    /// Catches: the length measured before trimming.
    /// </summary>
    [Fact]
    public void AddTask_100CharactersPlusSurroundingSpaces_IsAccepted()
    {
        var hundred = new string('a', 100);

        var task = AddTask(OpenGrant(), title: $"  {hundred}  ");

        Assert.Equal(hundred, task.Title);
    }

    /// <summary>
    /// BR3-6: an empty title with a past due date reports title-required, not a due-date error.
    /// Catches: the due-date check running before BR-3.
    /// </summary>
    [Fact]
    public void AddTask_EmptyTitleAndPastDueDate_ReportsTheTitleFirst()
    {
        var grant = OpenGrant();

        var error = Assert.Throws<DomainRuleException>(() => AddTask(grant, title: "", dueDate: Today.AddDays(-1)));

        Assert.Equal(ErrorCodes.TitleRequired, error.Code);
    }
}
