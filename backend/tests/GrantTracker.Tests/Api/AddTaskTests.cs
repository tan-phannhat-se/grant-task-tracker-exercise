using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using GrantTracker.Api.Data;
using Microsoft.Extensions.DependencyInjection;
using static GrantTracker.Tests.Api.GrantTrackerApiFactory;

namespace GrantTracker.Tests.Api;

/// <summary>
/// POST /grants/{id}/tasks: a valid task is saved and returned; rule errors and missing fields come back as 4xx.
/// </summary>
public class AddTaskTests
{
    /// <summary>
    /// AT-1: a valid task on GR-100002, due today → 201 without Location, title trimmed, not overdue,
    /// and the grant then lists it.
    /// Catches: the task not saved, 200 instead of 201, or the real clock used for BR-2 (due today would be refused).
    /// </summary>
    [Fact]
    public async Task Add_ValidTask_Returns201AndSavesIt()
    {
        using var api = new GrantTrackerApiFactory();
        var grantId = GrantIdOf(api, "GR-100002");

        var response = await PostTask(api, grantId, $$"""
            { "title": "  Call applicant  ", "dueDate": "{{Day(0)}}", "priority": "High" }
            """);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Null(response.Headers.Location);
        var task = await ReadJson(response);
        Assert.Equal("Call applicant", task.GetProperty("title").GetString());
        Assert.Equal(Day(0), task.GetProperty("dueDate").GetString());
        Assert.Equal("High", task.GetProperty("priority").GetString());
        Assert.Equal(grantId, task.GetProperty("grantId").GetInt32());
        Assert.False(task.GetProperty("isOverdue").GetBoolean());

        var grant = await ReadJson(await api.CreateClient().GetAsync($"/grants/{grantId}"));
        Assert.Contains(
            grant.GetProperty("tasks").EnumerateArray(),
            t => t.GetProperty("id").GetInt32() == task.GetProperty("id").GetInt32());
    }

    /// <summary>
    /// AT-2: the new task's CreatedAt is the injected clock's time.
    /// Catches: the service reading the real clock instead of TimeProvider.
    /// </summary>
    [Fact]
    public async Task Add_ValidTask_StoresCreatedAtFromTheInjectedClock()
    {
        using var api = new GrantTrackerApiFactory();
        var grantId = GrantIdOf(api, "GR-100002");

        await PostTask(api, grantId, $$"""
            { "title": "Clock check", "dueDate": "{{Day(1)}}", "priority": "Low" }
            """);

        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GrantTrackerDbContext>();
        var task = db.GrantTasks.Single(t => t.Title == "Clock check");
        Assert.Equal(Now.UtcDateTime, task.CreatedAt);
    }

    /// <summary>
    /// AT-3: a task on the closed grant GR-100003 → 409 grant-closed.
    /// Catches: a domain rule error not reaching the HTTP response (BR-1 through the API).
    /// </summary>
    [Fact]
    public async Task Add_ToClosedGrant_Returns409()
    {
        using var api = new GrantTrackerApiFactory();

        var response = await PostTask(api, GrantIdOf(api, "GR-100003"), $$"""
            { "title": "Too late", "dueDate": "{{Day(1)}}", "priority": "Low" }
            """);

        await AssertProblem(response, HttpStatusCode.Conflict, "grant-closed", field: null);
    }

    /// <summary>
    /// AT-9: a task due yesterday on GR-100002 → 400 due-date-in-past on field dueDate, naming today; nothing saved.
    /// Catches: the service passing the domain a wrong "today" (e.g. too early), so a past due date is accepted.
    /// </summary>
    [Fact]
    public async Task Add_DueDateYesterday_Returns400DueDateInPast()
    {
        using var api = new GrantTrackerApiFactory();

        var response = await PostTask(api, GrantIdOf(api, "GR-100002"), $$"""
            { "title": "Yesterday", "dueDate": "{{Day(-1)}}", "priority": "Low" }
            """);

        await AssertProblem(response, HttpStatusCode.BadRequest, "due-date-in-past", "dueDate");
        var problem = await ReadJson(response);
        Assert.Contains(Day(0), problem.GetProperty("detail").GetString());
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GrantTrackerDbContext>();
        Assert.False(db.GrantTasks.Any(t => t.Title == "Yesterday"));
    }

    /// <summary>
    /// AT-4: no title in the JSON → 400 title-required on field title.
    /// Catches: a null title crashing with a 500.
    /// </summary>
    [Fact]
    public async Task Add_WithoutTitle_Returns400TitleRequired()
    {
        using var api = new GrantTrackerApiFactory();

        var response = await PostTask(api, GrantIdOf(api, "GR-100002"), $$"""
            { "dueDate": "{{Day(1)}}", "priority": "Low" }
            """);

        await AssertProblem(response, HttpStatusCode.BadRequest, "title-required", "title");
    }

    /// <summary>
    /// AT-5: no dueDate in the JSON → 400 due-date-required on field dueDate.
    /// Catches: a missing date slipping through as a default date.
    /// </summary>
    [Fact]
    public async Task Add_WithoutDueDate_Returns400DueDateRequired()
    {
        using var api = new GrantTrackerApiFactory();

        var response = await PostTask(api, GrantIdOf(api, "GR-100002"), """
            { "title": "No date", "priority": "Low" }
            """);

        await AssertProblem(response, HttpStatusCode.BadRequest, "due-date-required", "dueDate");
    }

    /// <summary>
    /// AT-6: no priority in the JSON → 400 priority-invalid on field priority.
    /// Catches: a task saved without a priority.
    /// </summary>
    [Fact]
    public async Task Add_WithoutPriority_Returns400PriorityInvalid()
    {
        using var api = new GrantTrackerApiFactory();

        var response = await PostTask(api, GrantIdOf(api, "GR-100002"), $$"""
            { "title": "No priority", "dueDate": "{{Day(1)}}" }
            """);

        await AssertProblem(response, HttpStatusCode.BadRequest, "priority-invalid", "priority");
    }

    /// <summary>
    /// AT-7: a grant id that does not exist → 404 grant-not-found.
    /// Catches: a 500 when the grant is missing.
    /// </summary>
    [Fact]
    public async Task Add_ToUnknownGrant_Returns404()
    {
        using var api = new GrantTrackerApiFactory();

        var response = await PostTask(api, 9999, $$"""
            { "title": "Nowhere", "dueDate": "{{Day(1)}}", "priority": "Low" }
            """);

        await AssertProblem(response, HttpStatusCode.NotFound, "grant-not-found", field: null);
    }

    /// <summary>
    /// AT-8: broken JSON → 400.
    /// Catches: a body that cannot be read crashing with a 500.
    /// </summary>
    [Fact]
    public async Task Add_BrokenJson_Returns400()
    {
        using var api = new GrantTrackerApiFactory();

        var response = await PostTask(api, GrantIdOf(api, "GR-100002"), "{ \"title\": ");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static string Day(int daysFromToday) =>
        Today.AddDays(daysFromToday).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static int GrantIdOf(GrantTrackerApiFactory api, string referenceNumber)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GrantTrackerDbContext>();
        return db.Grants.Single(g => g.ReferenceNumber == referenceNumber).Id;
    }

    private static Task<HttpResponseMessage> PostTask(GrantTrackerApiFactory api, int grantId, string json) =>
        api.CreateClient().PostAsync(
            $"/grants/{grantId}/tasks",
            new StringContent(json, Encoding.UTF8, "application/json"));

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();

    private static async Task AssertProblem(
        HttpResponseMessage response,
        HttpStatusCode status,
        string code,
        string? field)
    {
        Assert.Equal(status, response.StatusCode);
        var problem = await ReadJson(response);
        Assert.Equal(code, problem.GetProperty("code").GetString());
        if (field is null)
        {
            Assert.False(problem.TryGetProperty("field", out _));
        }
        else
        {
            Assert.Equal(field, problem.GetProperty("field").GetString());
        }
    }
}
