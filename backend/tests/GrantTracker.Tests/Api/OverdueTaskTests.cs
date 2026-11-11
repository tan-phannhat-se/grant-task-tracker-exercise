using System.Globalization;
using System.Net;
using System.Text.Json;
using GrantTracker.Api.Data;
using GrantTracker.Api.Domain;
using Microsoft.Extensions.DependencyInjection;
using static GrantTracker.Tests.Api.GrantTrackerApiFactory;

namespace GrantTracker.Tests.Api;

/// <summary>
/// GET /tasks/overdue: overdue tasks across grants, most overdue first, open grants unless asked otherwise.
/// </summary>
public class OverdueTaskTests
{
    /// <summary>
    /// OD-1: no filter → the two seeded overdue tasks, most overdue first, with their grant and days overdue.
    /// Catches: a wrong order, completed or not-yet-due tasks listed, daysOverdue off by one, or the real clock.
    /// </summary>
    [Fact]
    public async Task List_NoFilter_ReturnsOverdueTasksMostOverdueFirst()
    {
        using var api = new GrantTrackerApiFactory();

        var rows = await GetOverdue(api, "");

        (string Title, string Reference, string Organisation, string Due, int Days)[] expected =
        {
            ("Verify bank details", "GR-100001", "Riverside Community Garden Inc.", Day(-3), 3),
            ("Review progress report", "GR-100002", "Northside Youth Sports Club", Day(-1), 1),
        };
        var actual = rows.Select(r => (
                r.GetProperty("title").GetString()!,
                r.GetProperty("referenceNumber").GetString()!,
                r.GetProperty("organisationName").GetString()!,
                r.GetProperty("dueDate").GetString()!,
                r.GetProperty("daysOverdue").GetInt32()))
            .ToArray();
        Assert.Equal(expected, actual);
        Assert.All(rows, r => Assert.True(r.GetProperty("isOverdue").GetBoolean()));
    }

    /// <summary>
    /// OD-2: an overdue task on a closed grant is left out when no filter is given.
    /// Catches: the default being All instead of Open.
    /// </summary>
    [Fact]
    public async Task List_NoFilter_LeavesOutClosedGrants()
    {
        using var api = new GrantTrackerApiFactory();
        AddClosedGrantWithOverdueTask(api);

        var rows = await GetOverdue(api, "");

        Assert.DoesNotContain(rows, r => r.GetProperty("referenceNumber").GetString() == "GR-200001");
    }

    /// <summary>
    /// OD-3: with the same closed grant, ?grantStatus=Closed → only its task; ?grantStatus=All → all three.
    /// Catches: the grantStatus filter ignored.
    /// </summary>
    [Fact]
    public async Task List_GrantStatusClosedOrAll_FiltersByGrantStatus()
    {
        using var api = new GrantTrackerApiFactory();
        AddClosedGrantWithOverdueTask(api);

        var closed = await GetOverdue(api, "?grantStatus=Closed");
        var all = await GetOverdue(api, "?grantStatus=All");

        var closedTask = Assert.Single(closed);
        Assert.Equal("GR-200001", closedTask.GetProperty("referenceNumber").GetString());
        Assert.Equal(3, all.Length);
    }

    /// <summary>
    /// OD-4: ?grantStatus=Foo → 400 status-invalid on field grantStatus.
    /// Catches: a bad filter silently returning a list.
    /// </summary>
    [Fact]
    public async Task List_InvalidGrantStatus_IsRejected()
    {
        using var api = new GrantTrackerApiFactory();

        var response = await api.CreateClient().GetAsync("/tasks/overdue?grantStatus=Foo");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("status-invalid", problem.GetProperty("code").GetString());
        Assert.Equal("grantStatus", problem.GetProperty("field").GetString());
    }

    private static string Day(int daysFromToday) =>
        Today.AddDays(daysFromToday).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    // A closed grant can only have an overdue task if the task was added and left pending before the close.
    private static void AddClosedGrantWithOverdueTask(GrantTrackerApiFactory api)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GrantTrackerDbContext>();
        var grant = new Grant("GR-200001", "Closed Test Organisation", Today.AddDays(-20));
        grant.AddTask("Left over", Today.AddDays(-5), Priority.Low, Today.AddDays(-20), Now.UtcDateTime);
        grant.Close();
        db.Grants.Add(grant);
        db.SaveChanges();
    }

    private static async Task<JsonElement[]> GetOverdue(GrantTrackerApiFactory api, string query)
    {
        var response = await api.CreateClient().GetAsync("/tasks/overdue" + query);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        return body.EnumerateArray().Select(r => r.Clone()).ToArray();
    }
}
