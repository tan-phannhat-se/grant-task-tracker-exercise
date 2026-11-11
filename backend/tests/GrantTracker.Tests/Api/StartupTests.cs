using GrantTracker.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GrantTracker.Tests.Api;

/// <summary>
/// What Program.cs does at start-up: seed in Development only, with the injected clock, and fail on a bad time zone.
/// </summary>
public class StartupTests
{
    /// <summary>
    /// AF-1: the test API starts in Development with the fake clock → seed dates are relative to 15 Jan 2020.
    /// Catches: the fixture not replacing the clock, so every later API test would depend on the real date.
    /// </summary>
    [Fact]
    public void Start_Development_SeedsWithTheInjectedClock()
    {
        using var api = new GrantTrackerApiFactory();

        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GrantTrackerDbContext>();
        var hillview = db.Grants.Single(g => g.ReferenceNumber == "GR-100004");

        Assert.Equal(GrantTrackerApiFactory.Today.AddDays(-5), hillview.ApplicationDate);
    }

    /// <summary>
    /// AF-2: the API starts in Production → the database has no grants.
    /// Catches: the fictional sample data seeded outside Development.
    /// </summary>
    [Fact]
    public void Start_Production_DoesNotSeed()
    {
        using var api = new GrantTrackerApiFactory();
        using var production = api.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));

        using var scope = production.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GrantTrackerDbContext>();

        Assert.Empty(db.Grants);
    }

    /// <summary>
    /// AF-3: an unknown App:TimeZone in Production → the API fails to start with TimeZoneNotFoundException.
    /// Production, because there the seed does not create the calendar first and hide the bug.
    /// Catches: the calendar created only on the first request, so a bad setting would pass the deploy.
    /// </summary>
    [Fact]
    public void Start_UnknownTimeZone_FailsAtStartUp()
    {
        using var api = new GrantTrackerApiFactory();
        using var badTimeZone = api.WithWebHostBuilder(builder => builder
            .UseEnvironment("Production")
            .ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["App:TimeZone"] = "Mars/Olympus" })));

        Assert.Throws<TimeZoneNotFoundException>(() => badTimeZone.Services);
    }
}
