using GrantTracker.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace GrantTracker.Tests.Api;

/// <summary>
/// Runs the whole API in memory for a test, with its own empty database and a fixed clock.
/// </summary>
public sealed class GrantTrackerApiFactory : WebApplicationFactory<Program>
{
    // A past instant, so a test can never pass by reading the real clock.
    public static readonly DateTimeOffset Now = new(2020, 1, 15, 9, 0, 0, TimeSpan.Zero);
    public static readonly DateOnly Today = new(2020, 1, 15);

    // An in-memory database lives as long as its connection, so one open connection per factory
    // gives every test its own database.
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    // The app's clock; a test can move it, e.g. to the next day.
    public FakeTimeProvider Clock { get; } = new(Now);

    /// <summary>
    /// Opens the connection the test database lives on.
    /// </summary>
    public GrantTrackerApiFactory()
    {
        _connection.Open();
    }

    /// <summary>
    /// Replaces the database and the clock; everything else runs as in Program.cs.
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<GrantTrackerDbContext>>();
            services.AddDbContext<GrantTrackerDbContext>(options => options.UseSqlite(_connection));

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }

    /// <summary>
    /// Disposes the API, then the connection, which deletes the in-memory database.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
