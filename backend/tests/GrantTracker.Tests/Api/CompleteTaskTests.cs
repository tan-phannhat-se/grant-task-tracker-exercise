using System.Globalization;
using System.Net;
using System.Text.Json;
using GrantTracker.Api.Data;
using Microsoft.Extensions.DependencyInjection;
using static GrantTracker.Tests.Api.GrantTrackerApiFactory;

namespace GrantTracker.Tests.Api;

/// <summary>
/// PUT /tasks/{id}/complete: records today's date once; a repeat returns 200 and keeps that date.
/// </summary>
public class CompleteTaskTests
{
    /// <summary>
    /// CT-1: completing the overdue "Verify bank details" → 200, completed today, no longer overdue,
    /// completed late, and the grant then shows it completed.
    /// Catches: the completion not saved, the date taken from the real clock, or a wrong status code.
    /// </summary>
    [Fact]
    public async Task Complete_PendingTask_Returns200AndSavesTodaysDate()
    {
        using var api = new GrantTrackerApiFactory();
        var (taskId, grantId) = TaskIdOf(api, "Verify bank details");

        var response = await api.CreateClient().PutAsync($"/tasks/{taskId}/complete", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var task = await ReadJson(response);
        Assert.Equal(Day(0), task.GetProperty("completedDate").GetString());
        Assert.False(task.GetProperty("isOverdue").GetBoolean());
        Assert.True(task.GetProperty("completedLate").GetBoolean());

        var grant = await ReadJson(await api.CreateClient().GetAsync($"/grants/{grantId}"));
        var saved = grant.GetProperty("tasks").EnumerateArray().Single(t => t.GetProperty("id").GetInt32() == taskId);
        Assert.Equal(Day(0), saved.GetProperty("completedDate").GetString());
    }

    /// <summary>
    /// CT-2: complete, move the clock to the next day, complete again → 200 and the first date is kept.
    /// Catches: a repeat overwriting the date (BR-4 through the API), or a repeat refused with 409.
    /// </summary>
    [Fact]
    public async Task Complete_Twice_KeepsTheFirstDate()
    {
        using var api = new GrantTrackerApiFactory();
        var (taskId, _) = TaskIdOf(api, "Verify bank details");
        await api.CreateClient().PutAsync($"/tasks/{taskId}/complete", null);
        api.Clock.Advance(TimeSpan.FromDays(1));

        var response = await api.CreateClient().PutAsync($"/tasks/{taskId}/complete", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var task = await ReadJson(response);
        Assert.Equal(Day(0), task.GetProperty("completedDate").GetString());
    }

    /// <summary>
    /// CT-3: a task id that does not exist → 404 with code task-not-found.
    /// Catches: a 500 when the task is missing.
    /// </summary>
    [Fact]
    public async Task Complete_UnknownTask_Returns404()
    {
        using var api = new GrantTrackerApiFactory();

        var response = await api.CreateClient().PutAsync("/tasks/9999/complete", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await ReadJson(response);
        Assert.Equal("task-not-found", problem.GetProperty("code").GetString());
    }

    private static string Day(int daysFromToday) =>
        Today.AddDays(daysFromToday).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static (int TaskId, int GrantId) TaskIdOf(GrantTrackerApiFactory api, string title)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GrantTrackerDbContext>();
        var task = db.GrantTasks.Single(t => t.Title == title);
        return (task.Id, task.GrantId);
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
}
