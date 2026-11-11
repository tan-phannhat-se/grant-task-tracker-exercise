using GrantTracker.Api.Domain;
using static GrantTracker.Tests.Domain.TestData;

namespace GrantTracker.Tests.Domain;

/// <summary>
/// Fields that AddTask must store exactly as given: the idempotency key (OP-3) and the creation time (OP-6).
/// </summary>
public class StoredFieldsTests
{
    /// <summary>
    /// ST-1: the idempotency key passed to AddTask is stored on the task.
    /// Catches: the key dropped, which would break safe retries (OP-3).
    /// </summary>
    [Fact]
    public void AddTask_WithIdempotencyKey_StoresTheKey()
    {
        var task = OpenGrant().AddTask("Check docs", Today.AddDays(7), Priority.Medium, Today, NowUtc, "k-1");

        Assert.Equal("k-1", task.IdempotencyKey);
    }

    /// <summary>
    /// ST-2: the creation time passed to AddTask is stored as CreatedAt.
    /// Catches: the real clock used instead of the given time.
    /// </summary>
    [Fact]
    public void AddTask_StoresTheGivenCreationTime()
    {
        var task = AddTask(OpenGrant());

        Assert.Equal(NowUtc, task.CreatedAt);
    }
}
