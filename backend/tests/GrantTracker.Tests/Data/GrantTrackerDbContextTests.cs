using GrantTracker.Api.Data;
using GrantTracker.Api.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using static GrantTracker.Tests.Domain.TestData;

namespace GrantTracker.Tests.Data;

/// <summary>
/// The EF Core mapping against a real SQLite database: fields, unique indexes and delete behaviour.
/// </summary>
public sealed class GrantTrackerDbContextTests : IDisposable
{
    private const string IdempotencyKey = "3f2b6c1e-8d4a-4f0b-9c7e-2a1d5e6f7a8b";

    // An in-memory database lives as long as its connection, so one open connection per test
    // gives every test its own empty database.
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public GrantTrackerDbContextTests()
    {
        _connection.Open();
        using var db = NewContext();
        db.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    /// <summary>
    /// DB-1: a closed grant with a completed and a pending task, saved and read back in a new context,
    /// keeps every field, and CreatedAt is still UTC.
    /// Catches: a field not mapped, tasks not saved through the grant, or CreatedAt read back without the UTC kind.
    /// </summary>
    [Fact]
    public void SaveGrantWithTasks_ReadBack_KeepsEveryField()
    {
        var grant = new Grant("GR-100001", "Test Organisation", Today.AddDays(-30));
        var completedTask = grant.AddTask(
            "Check budget", Today.AddDays(7), Priority.High, Today, NowUtc, IdempotencyKey);
        var pendingTask = grant.AddTask(
            "Call applicant", Today.AddDays(3), Priority.Low, Today, NowUtc);
        completedTask.Complete(Today);
        grant.Close();
        Save(grant);

        using var db = NewContext();
        var saved = db.Grants.Include(g => g.Tasks).Single();

        Assert.Equal("GR-100001", saved.ReferenceNumber);
        Assert.Equal("Test Organisation", saved.OrganisationName);
        Assert.Equal(Today.AddDays(-30), saved.ApplicationDate);
        Assert.Equal(GrantStatus.Closed, saved.Status);
        Assert.Equal(2, saved.Tasks.Count);
        AssertSameTask(completedTask, saved.Tasks.Single(t => t.Title == "Check budget"), saved.Id);
        AssertSameTask(pendingTask, saved.Tasks.Single(t => t.Title == "Call applicant"), saved.Id);
    }

    /// <summary>
    /// DB-2: a second grant with the same reference number is refused by the database.
    /// Catches: the unique index on the reference number missing.
    /// </summary>
    [Fact]
    public void SaveGrant_DuplicateReferenceNumber_IsRefused()
    {
        using var db = NewContext();
        db.Grants.Add(new Grant("GR-100001", "First Organisation", Today));
        db.Grants.Add(new Grant("GR-100001", "Second Organisation", Today));

        Assert.Throws<DbUpdateException>(() => db.SaveChanges());
    }

    /// <summary>
    /// DB-3: two tasks with no idempotency key are saved; two tasks with the same key are refused.
    /// Catches: the unique index on the key missing, or a missing key saved as "" so keyless tasks clash.
    /// </summary>
    [Fact]
    public void SaveTasks_SameIdempotencyKey_IsRefusedButNoKeyIsAllowed()
    {
        using var db = NewContext();
        var grant = new Grant("GR-100001", "Test Organisation", Today);
        db.Grants.Add(grant);
        grant.AddTask("No key 1", Today, Priority.Low, Today, NowUtc);
        grant.AddTask("No key 2", Today, Priority.Low, Today, NowUtc);
        db.SaveChanges();

        grant.AddTask("Same key 1", Today, Priority.Low, Today, NowUtc, IdempotencyKey);
        grant.AddTask("Same key 2", Today, Priority.Low, Today, NowUtc, IdempotencyKey);

        Assert.Throws<DbUpdateException>(() => db.SaveChanges());
    }

    /// <summary>
    /// DB-4: deleting a grant that still has tasks is refused, and the tasks are still there.
    /// Catches: cascade delete (EF's default for a required relationship) removing the tasks silently.
    /// </summary>
    [Fact]
    public void DeleteGrant_WithTasks_IsRefused()
    {
        var grant = new Grant("GR-100001", "Test Organisation", Today);
        grant.AddTask("Check budget", Today, Priority.Low, Today, NowUtc);
        Save(grant);

        using (var db = NewContext())
        {
            db.Grants.Remove(db.Grants.Single());

            Assert.Throws<DbUpdateException>(() => db.SaveChanges());
        }

        using var check = NewContext();
        Assert.Equal(1, check.GrantTasks.Count());
    }

    private GrantTrackerDbContext NewContext() =>
        new(new DbContextOptionsBuilder<GrantTrackerDbContext>().UseSqlite(_connection).Options);

    private void Save(Grant grant)
    {
        using var db = NewContext();
        db.Grants.Add(grant);
        db.SaveChanges();
    }

    private static void AssertSameTask(GrantTask expected, GrantTask actual, int grantId)
    {
        Assert.Equal(grantId, actual.GrantId);
        Assert.Equal(expected.Title, actual.Title);
        Assert.Equal(expected.DueDate, actual.DueDate);
        Assert.Equal(expected.Priority, actual.Priority);
        Assert.Equal(expected.CompletedDate, actual.CompletedDate);
        Assert.Equal(expected.IdempotencyKey, actual.IdempotencyKey);
        Assert.Equal(expected.CreatedAt, actual.CreatedAt);
        // DateTime equality ignores Kind, so the UTC kind is checked on its own.
        Assert.Equal(DateTimeKind.Utc, actual.CreatedAt.Kind);
    }
}
