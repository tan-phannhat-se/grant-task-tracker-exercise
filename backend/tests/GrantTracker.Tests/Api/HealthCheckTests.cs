using System.Net;
using GrantTracker.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GrantTracker.Tests.Api;

/// <summary>
/// GET /health: tells the operations team whether this copy of the API works and can reach its data.
/// </summary>
public class HealthCheckTests
{
    /// <summary>
    /// HC-1: the database is reachable → 200.
    /// Catches: the route missing, or a working copy reported as unhealthy.
    /// </summary>
    [Fact]
    public async Task Health_DatabaseReachable_Returns200()
    {
        using var api = new GrantTrackerApiFactory();

        var response = await api.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// HC-2: the tables are gone after start-up → 503.
    /// Catches: a check that never touches the data, so a copy that has lost its data is still sent officers.
    /// </summary>
    [Fact]
    public async Task Health_DataNotReachable_Returns503()
    {
        using var api = new GrantTrackerApiFactory();
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GrantTrackerDbContext>();
            db.Database.ExecuteSqlRaw("DROP TABLE GrantTasks");
            db.Database.ExecuteSqlRaw("DROP TABLE Grants");
        }

        var response = await api.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}
