using System.Net;
using System.Text.Json;
using GrantTracker.Api.Data;
using Microsoft.Extensions.DependencyInjection;

namespace GrantTracker.Tests.Api;

/// <summary>
/// PUT /grants/{id}/close: closes a grant unless a High-priority task is pending; closing again changes nothing.
/// </summary>
public class CloseGrantTests
{
    /// <summary>
    /// CG-1: GR-100002 (only Low and Medium tasks pending) → 200, status Closed with its tasks,
    /// and the closed list then includes it.
    /// Catches: the close not saved, or a wrong status code.
    /// </summary>
    [Fact]
    public async Task Close_GrantWithoutPendingHighTask_Returns200AndSavesIt()
    {
        using var api = new GrantTrackerApiFactory();
        var grantId = GrantIdOf(api, "GR-100002");

        var response = await api.CreateClient().PutAsync($"/grants/{grantId}/close", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var grant = await ReadJson(response);
        Assert.Equal("Closed", grant.GetProperty("status").GetString());
        Assert.Equal(2, grant.GetProperty("tasks").GetArrayLength());

        var closed = await ReadJson(await api.CreateClient().GetAsync("/grants?status=Closed"));
        Assert.Contains(closed.EnumerateArray(), g => g.GetProperty("id").GetInt32() == grantId);
    }

    /// <summary>
    /// CG-2: GR-100001 (High "Verify bank details" pending) → 409 pending-high-priority-tasks naming that task,
    /// and the grant stays Open.
    /// Catches: the grant loaded without its tasks, so BR-5 sees none and lets the close through.
    /// </summary>
    [Fact]
    public async Task Close_GrantWithPendingHighTask_Returns409AndStaysOpen()
    {
        using var api = new GrantTrackerApiFactory();
        var grantId = GrantIdOf(api, "GR-100001");

        var response = await api.CreateClient().PutAsync($"/grants/{grantId}/close", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await ReadJson(response);
        Assert.Equal("pending-high-priority-tasks", problem.GetProperty("code").GetString());
        Assert.Contains("Verify bank details", problem.GetProperty("detail").GetString());

        var grant = await ReadJson(await api.CreateClient().GetAsync($"/grants/{grantId}"));
        Assert.Equal("Open", grant.GetProperty("status").GetString());
    }

    /// <summary>
    /// CG-3: the already closed GR-100003 → 200 and still Closed.
    /// Catches: closing again refused with 409 instead of the agreed 200.
    /// </summary>
    [Fact]
    public async Task Close_AlreadyClosedGrant_Returns200Unchanged()
    {
        using var api = new GrantTrackerApiFactory();

        var response = await api.CreateClient().PutAsync($"/grants/{GrantIdOf(api, "GR-100003")}/close", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var grant = await ReadJson(response);
        Assert.Equal("Closed", grant.GetProperty("status").GetString());
    }

    /// <summary>
    /// CG-4: a grant id that does not exist → 404 with code grant-not-found.
    /// Catches: a 500 when the grant is missing.
    /// </summary>
    [Fact]
    public async Task Close_UnknownGrant_Returns404()
    {
        using var api = new GrantTrackerApiFactory();

        var response = await api.CreateClient().PutAsync("/grants/9999/close", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await ReadJson(response);
        Assert.Equal("grant-not-found", problem.GetProperty("code").GetString());
    }

    private static int GrantIdOf(GrantTrackerApiFactory api, string referenceNumber)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GrantTrackerDbContext>();
        return db.Grants.Single(g => g.ReferenceNumber == referenceNumber).Id;
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
}
