using System.Globalization;
using System.Net;
using System.Text.Json;
using static GrantTracker.Tests.Api.GrantTrackerApiFactory;

namespace GrantTracker.Tests.Api;

/// <summary>
/// GET /grants on the seeded data: the list, its pending task counts and the status filter.
/// </summary>
public class GrantListTests
{
    /// <summary>
    /// GL-1: no filter → the four seeded grants by reference number, dates as yyyy-MM-dd, status by name,
    /// pending task counts 2, 2, 0, 0.
    /// Catches: completed tasks counted as pending, the status sent as a number, or an unstable order.
    /// </summary>
    [Fact]
    public async Task List_NoFilter_ReturnsEveryGrantWithItsPendingTaskCount()
    {
        (string Reference, string Organisation, string Applied, string Status, int Pending)[] expected =
        {
            ("GR-100001", "Riverside Community Garden Inc.", Day(-60), "Open", 2),
            ("GR-100002", "Northside Youth Sports Club", Day(-20), "Open", 2),
            ("GR-100003", "Harbour Arts Collective", Day(-200), "Closed", 0),
            ("GR-100004", "Hillview Library Friends Group", Day(-5), "Open", 0),
        };

        var grants = await GetGrants("");

        var actual = grants.Select(g => (
                g.GetProperty("referenceNumber").GetString()!,
                g.GetProperty("organisationName").GetString()!,
                g.GetProperty("applicationDate").GetString()!,
                g.GetProperty("status").GetString()!,
                g.GetProperty("pendingTaskCount").GetInt32()))
            .ToArray();
        Assert.Equal(expected, actual);
        Assert.All(grants, g => Assert.True(g.GetProperty("id").GetInt32() > 0));
    }

    /// <summary>
    /// GL-2: ?status=Open → only the three open grants.
    /// Catches: the filter ignored.
    /// </summary>
    [Fact]
    public async Task List_StatusOpen_ReturnsOnlyOpenGrants()
    {
        var grants = await GetGrants("?status=Open");

        Assert.Equal(new[] { "GR-100001", "GR-100002", "GR-100004" }, References(grants));
    }

    /// <summary>
    /// GL-3: ?status=Closed → only GR-100003.
    /// Catches: the filter inverted, or always filtering on Open.
    /// </summary>
    [Fact]
    public async Task List_StatusClosed_ReturnsOnlyClosedGrants()
    {
        var grants = await GetGrants("?status=Closed");

        Assert.Equal(new[] { "GR-100003" }, References(grants));
    }

    /// <summary>
    /// GL-4: an unknown status or a number → 400 with code status-invalid on field status.
    /// Catches: a bad filter silently returning every grant, or a number bound to an enum value.
    /// </summary>
    [Theory]
    [InlineData("Foo")]
    [InlineData("100")]
    public async Task List_InvalidStatus_IsRejected(string status)
    {
        using var api = new GrantTrackerApiFactory();

        var response = await api.CreateClient().GetAsync($"/grants?status={status}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("status-invalid", problem.GetProperty("code").GetString());
        Assert.Equal("status", problem.GetProperty("field").GetString());
    }

    /// <summary>
    /// GL-5: ?status=all in lower case → every grant.
    /// Catches: All not accepted, or the filter matching case-sensitively.
    /// </summary>
    [Fact]
    public async Task List_StatusAllInLowerCase_ReturnsEveryGrant()
    {
        var grants = await GetGrants("?status=all");

        Assert.Equal(new[] { "GR-100001", "GR-100002", "GR-100003", "GR-100004" }, References(grants));
    }

    private static string Day(int daysFromToday) =>
        Today.AddDays(daysFromToday).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string[] References(JsonElement[] grants) =>
        grants.Select(g => g.GetProperty("referenceNumber").GetString()!).ToArray();

    private static async Task<JsonElement[]> GetGrants(string query)
    {
        using var api = new GrantTrackerApiFactory();

        var response = await api.CreateClient().GetAsync("/grants" + query);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        return body.EnumerateArray().Select(g => g.Clone()).ToArray();
    }
}
