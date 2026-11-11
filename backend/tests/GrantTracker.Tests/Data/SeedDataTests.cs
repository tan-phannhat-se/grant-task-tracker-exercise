using GrantTracker.Api.Data;
using GrantTracker.Api.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GrantTracker.Tests.Data;

/// <summary>
/// The seed matches the table in section 6 of the brief, with dates relative to the given "today".
/// </summary>
public sealed class SeedDataTests : IDisposable
{
    // A past date, so a seed that reads the real clock can never produce the expected dates.
    private static readonly DateOnly Today = new(2020, 1, 15);

    // An in-memory database lives as long as its connection, so one open connection per test
    // gives every test its own empty database.
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public SeedDataTests()
    {
        _connection.Open();
        using var db = NewContext();
        db.Database.EnsureCreated();
        SeedData.Seed(db, Today);
    }

    public void Dispose() => _connection.Dispose();

    /// <summary>
    /// SD-1: after seeding, a new context reads back the brief's four grants with their dates and status.
    /// Catches: a wrong offset or sign, a typo, a missing grant, GR-100003 left open, or the seed not saved.
    /// </summary>
    [Fact]
    public void Seed_AddsTheFourGrantsFromTheBrief()
    {
        (string Reference, string Organisation, DateOnly Applied, GrantStatus Status)[] expected =
        {
            ("GR-100001", "Riverside Community Garden Inc.", Today.AddDays(-60), GrantStatus.Open),
            ("GR-100002", "Northside Youth Sports Club", Today.AddDays(-20), GrantStatus.Open),
            ("GR-100003", "Harbour Arts Collective", Today.AddDays(-200), GrantStatus.Closed),
            ("GR-100004", "Hillview Library Friends Group", Today.AddDays(-5), GrantStatus.Open),
        };

        using var db = NewContext();
        var actual = db.Grants
            .OrderBy(g => g.ReferenceNumber)
            .AsEnumerable()
            .Select(g => (g.ReferenceNumber, g.OrganisationName, g.ApplicationDate, g.Status))
            .ToArray();

        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// SD-2: each grant has the brief's tasks; a Completed task was completed on its due date, a Pending one is not.
    /// Catches: a task on the wrong grant, a wrong priority or status, or completed "today" (it would look late).
    /// </summary>
    [Fact]
    public void Seed_AddsTheTasksFromTheBrief()
    {
        (string Grant, string Title, Priority Priority, DateOnly Due, DateOnly? Completed)[] expected =
        {
            ("GR-100001", "Check eligibility documents", Priority.High, Today.AddDays(-50), Today.AddDays(-50)),
            ("GR-100001", "Request quote for tools", Priority.Medium, Today.AddDays(5), null),
            ("GR-100001", "Verify bank details", Priority.High, Today.AddDays(-3), null),
            ("GR-100002", "Review progress report", Priority.Low, Today.AddDays(-1), null),
            ("GR-100002", "Schedule site visit", Priority.Medium, Today.AddDays(10), null),
            ("GR-100003", "Final acquittal review", Priority.High, Today.AddDays(-150), Today.AddDays(-150)),
        };

        using var db = NewContext();
        var actual = db.Grants
            .Include(g => g.Tasks)
            .AsEnumerable()
            .SelectMany(g => g.Tasks.Select(t => (g.ReferenceNumber, t.Title, t.Priority, t.DueDate, t.CompletedDate)))
            .OrderBy(t => t.ReferenceNumber)
            .ThenBy(t => t.Title)
            .ToArray();

        Assert.Equal(expected, actual);
    }

    private GrantTrackerDbContext NewContext() =>
        new(new DbContextOptionsBuilder<GrantTrackerDbContext>().UseSqlite(_connection).Options);
}
