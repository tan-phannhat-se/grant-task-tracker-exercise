using System.Globalization;
using System.Net;
using System.Text.Json;
using GrantTracker.Api.Data;
using GrantTracker.Api.Domain;
using Microsoft.Extensions.DependencyInjection;
using static GrantTracker.Tests.Api.GrantTrackerApiFactory;

namespace GrantTracker.Tests.Api;

/// <summary>
/// GET /grants/{id}: one grant with its tasks, overdue and completed-late flags computed by the server.
/// </summary>
public class GrantDetailTests
{
    /// <summary>
    /// GD-1: GR-100001 → 200 with its fields and 3 tasks by due date: completed, overdue, not yet due.
    /// Catches: tasks not loaded, isOverdue wrong or from the real clock, a wrong order, or priority as a number.
    /// </summary>
    [Fact]
    public async Task Get_SeededGrant_ReturnsItsTasksByDueDate()
    {
        using var api = new GrantTrackerApiFactory();
        var grantId = GrantIdOf(api, "GR-100001");

        var grant = await GetGrant(api, grantId);

        Assert.Equal(grantId, grant.GetProperty("id").GetInt32());
        Assert.Equal("GR-100001", grant.GetProperty("referenceNumber").GetString());
        Assert.Equal("Riverside Community Garden Inc.", grant.GetProperty("organisationName").GetString());
        Assert.Equal(Day(-60), grant.GetProperty("applicationDate").GetString());
        Assert.Equal("Open", grant.GetProperty("status").GetString());

        (string Title, string Priority, string Due, string? Completed, bool Overdue, bool Late)[] expected =
        {
            ("Check eligibility documents", "High", Day(-50), Day(-50), false, false),
            ("Verify bank details", "High", Day(-3), null, true, false),
            ("Request quote for tools", "Medium", Day(5), null, false, false),
        };
        var tasks = grant.GetProperty("tasks").EnumerateArray().ToArray();
        var actual = tasks.Select(t => (
                t.GetProperty("title").GetString()!,
                t.GetProperty("priority").GetString()!,
                t.GetProperty("dueDate").GetString()!,
                t.GetProperty("completedDate").GetString(),
                t.GetProperty("isOverdue").GetBoolean(),
                t.GetProperty("completedLate").GetBoolean()))
            .ToArray();
        Assert.Equal(expected, actual);
        Assert.All(tasks, t => Assert.Equal(grantId, t.GetProperty("grantId").GetInt32()));
        Assert.All(tasks, t => Assert.True(t.GetProperty("id").GetInt32() > 0));
    }

    /// <summary>
    /// GD-2: a task completed after its due date → completedLate is true.
    /// Catches: completedLate not mapped from the domain, or always false.
    /// </summary>
    [Fact]
    public async Task Get_TaskCompletedAfterItsDueDate_IsCompletedLate()
    {
        using var api = new GrantTrackerApiFactory();
        int grantId;
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GrantTrackerDbContext>();
            var grant = new Grant("GR-200001", "Late Test Organisation", Today.AddDays(-10));
            var task = grant.AddTask("Late task", Today.AddDays(-8), Priority.Low, Today.AddDays(-10), Now.UtcDateTime);
            task.Complete(Today.AddDays(-5));
            db.Grants.Add(grant);
            db.SaveChanges();
            grantId = grant.Id;
        }

        var body = await GetGrant(api, grantId);

        var lateTask = Assert.Single(body.GetProperty("tasks").EnumerateArray());
        Assert.True(lateTask.GetProperty("completedLate").GetBoolean());
    }

    /// <summary>
    /// GD-3: an id that does not exist → 404 with code grant-not-found.
    /// Catches: a 500 for a missing grant, or a 200 with an empty body.
    /// </summary>
    [Fact]
    public async Task Get_UnknownId_Returns404()
    {
        using var api = new GrantTrackerApiFactory();

        var response = await api.CreateClient().GetAsync("/grants/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("grant-not-found", problem.GetProperty("code").GetString());
    }

    private static string Day(int daysFromToday) =>
        Today.AddDays(daysFromToday).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static int GrantIdOf(GrantTrackerApiFactory api, string referenceNumber)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GrantTrackerDbContext>();
        return db.Grants.Single(g => g.ReferenceNumber == referenceNumber).Id;
    }

    private static async Task<JsonElement> GetGrant(GrantTrackerApiFactory api, int grantId)
    {
        var response = await api.CreateClient().GetAsync($"/grants/{grantId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }
}
