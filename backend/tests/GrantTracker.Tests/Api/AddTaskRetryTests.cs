using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using GrantTracker.Api.Data;
using Microsoft.Extensions.DependencyInjection;
using static GrantTracker.Tests.Api.GrantTrackerApiFactory;

namespace GrantTracker.Tests.Api;

/// <summary>
/// POST /grants/{id}/tasks sent again with the same Idempotency-Key: safe to repeat, never a duplicate.
/// </summary>
public class AddTaskRetryTests
{
    private const string Key = "3f2b6c1e-8d4a-4f0b-9c7e-2a1d5e6f7a8b";

    /// <summary>
    /// RT-1: the same request and key sent twice → 201, then 200 with the same task; only one task is saved.
    /// Catches: a duplicate task, 201 on the retry, or the title compared before trimming
    /// (the stored title is trimmed, so a real retry would look different).
    /// </summary>
    [Fact]
    public async Task Add_SameKeyTwice_SecondReturns200WithTheSameTask()
    {
        using var api = new GrantTrackerApiFactory();
        var grantId = GrantIdOf(api, "GR-100002");
        var json = TaskJson("  Call applicant  ", "High");

        var first = await PostTask(api, grantId, json, Key);
        var second = await PostTask(api, grantId, json, Key);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var firstTask = await ReadJson(first);
        var secondTask = await ReadJson(second);
        Assert.Equal(firstTask.GetProperty("id").GetInt32(), secondTask.GetProperty("id").GetInt32());
        Assert.Equal("Call applicant", secondTask.GetProperty("title").GetString());
        Assert.Equal(1, CountTasksTitled(api, "Call applicant"));
    }

    /// <summary>
    /// RT-2: the same request sent twice without a key → 201 both times, two tasks.
    /// Catches: the key made mandatory, or "no key" treated as one shared key.
    /// </summary>
    [Fact]
    public async Task Add_NoKeyTwice_CreatesTwoTasks()
    {
        using var api = new GrantTrackerApiFactory();
        var grantId = GrantIdOf(api, "GR-100002");
        var json = TaskJson("No key", "Low");

        var first = await PostTask(api, grantId, json, idempotencyKey: null);
        var second = await PostTask(api, grantId, json, idempotencyKey: null);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.Equal(2, CountTasksTitled(api, "No key"));
    }

    /// <summary>
    /// RT-3: a key that is not a UUID → 400 idempotency-key-invalid, no task saved.
    /// Catches: any text accepted as a key ("not-a-uuid"), or only the length checked (36 characters, not a UUID).
    /// </summary>
    [Theory]
    [InlineData("not-a-uuid")]
    [InlineData("zzzzzzzz-zzzz-zzzz-zzzz-zzzzzzzzzzzz")]
    public async Task Add_KeyNotAUuid_Returns400(string badKey)
    {
        using var api = new GrantTrackerApiFactory();

        var response = await PostTask(api, GrantIdOf(api, "GR-100002"), TaskJson("Bad key", "Low"), badKey);

        await AssertProblem(response, HttpStatusCode.BadRequest, "idempotency-key-invalid");
        Assert.Equal(0, CountTasksTitled(api, "Bad key"));
    }

    /// <summary>
    /// RT-4: a key already used on GR-100002, sent with the same body to GR-100001 → 422 idempotency-key-reused,
    /// and GR-100001 gets no task.
    /// Catches: the other grant's task returned (one grant's data shown on another), or a second task created.
    /// </summary>
    [Fact]
    public async Task Add_KeyUsedOnAnotherGrant_Returns422()
    {
        using var api = new GrantTrackerApiFactory();
        var json = TaskJson("Shared key", "Low");
        await PostTask(api, GrantIdOf(api, "GR-100002"), json, Key);

        var response = await PostTask(api, GrantIdOf(api, "GR-100001"), json, Key);

        await AssertProblem(response, HttpStatusCode.UnprocessableEntity, "idempotency-key-reused");
        Assert.Equal(1, CountTasksTitled(api, "Shared key"));
    }

    /// <summary>
    /// RT-5: a key already used on the same grant, sent with a different title → 422 idempotency-key-reused.
    /// Catches: only the key compared, so the officer gets back a task they did not ask for.
    /// </summary>
    [Fact]
    public async Task Add_KeyUsedForADifferentTitle_Returns422()
    {
        using var api = new GrantTrackerApiFactory();
        var grantId = GrantIdOf(api, "GR-100002");
        await PostTask(api, grantId, TaskJson("First title", "Low"), Key);

        var response = await PostTask(api, grantId, TaskJson("Second title", "Low"), Key);

        await AssertProblem(response, HttpStatusCode.UnprocessableEntity, "idempotency-key-reused");
        Assert.Equal(0, CountTasksTitled(api, "Second title"));
    }

    /// <summary>
    /// RT-6: the same key sent twice to the closed grant GR-100003 → 409 grant-closed both times, no task saved.
    /// Catches: the key stored although the add was refused, so the retry returns a task that breaks BR-1.
    /// </summary>
    [Fact]
    public async Task Add_SameKeyTwiceToClosedGrant_Returns409BothTimes()
    {
        using var api = new GrantTrackerApiFactory();
        var grantId = GrantIdOf(api, "GR-100003");
        var json = TaskJson("Too late", "Low");

        var first = await PostTask(api, grantId, json, Key);
        var second = await PostTask(api, grantId, json, Key);

        await AssertProblem(first, HttpStatusCode.Conflict, "grant-closed");
        await AssertProblem(second, HttpStatusCode.Conflict, "grant-closed");
        Assert.Equal(0, CountTasksTitled(api, "Too late"));
    }

    /// <summary>
    /// RT-7: a task added with a key, the grant closed, then the request sent again → 200 with the same task.
    /// Catches: BR-1 checked before the key, so a request that worked is reported as refused.
    /// </summary>
    [Fact]
    public async Task Add_SameKeyAfterGrantClosed_Returns200WithTheSameTask()
    {
        using var api = new GrantTrackerApiFactory();
        var grantId = GrantIdOf(api, "GR-100002");
        var json = TaskJson("Before closing", "Low");
        var first = await PostTask(api, grantId, json, Key);
        var close = await api.CreateClient().PutAsync($"/grants/{grantId}/close", content: null);
        Assert.Equal(HttpStatusCode.OK, close.StatusCode);

        var retry = await PostTask(api, grantId, json, Key);

        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.Equal(
            (await ReadJson(first)).GetProperty("id").GetInt32(),
            (await ReadJson(retry)).GetProperty("id").GetInt32());
    }

    private static string TaskJson(string title, string priority) => $$"""
        { "title": "{{title}}", "dueDate": "{{Day(1)}}", "priority": "{{priority}}" }
        """;

    private static string Day(int daysFromToday) =>
        Today.AddDays(daysFromToday).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static int GrantIdOf(GrantTrackerApiFactory api, string referenceNumber)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GrantTrackerDbContext>();
        return db.Grants.Single(g => g.ReferenceNumber == referenceNumber).Id;
    }

    private static int CountTasksTitled(GrantTrackerApiFactory api, string title)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GrantTrackerDbContext>();
        return db.GrantTasks.Count(t => t.Title == title);
    }

    private static Task<HttpResponseMessage> PostTask(
        GrantTrackerApiFactory api,
        int grantId,
        string json,
        string? idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/grants/{grantId}/tasks")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        if (idempotencyKey is not null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        return api.CreateClient().SendAsync(request);
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();

    private static async Task AssertProblem(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        var problem = await ReadJson(response);
        Assert.Equal(code, problem.GetProperty("code").GetString());
        Assert.False(problem.TryGetProperty("field", out _)); // none of these errors belongs to a form input
    }
}
