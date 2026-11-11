using GrantTracker.Api.Data;
using GrantTracker.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI description + Swagger UI, so the endpoints can be tried from the browser.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<GrantTrackerDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("GrantTracker")));

// One clock for the whole app; the tests replace it with a fake one.
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddSingleton(services =>
{
    var timeZoneId = services.GetRequiredService<IConfiguration>()["App:TimeZone"] ?? "UTC";
    return new ConfiguredTimeZoneCalendar(
        services.GetRequiredService<TimeProvider>(),
        TimeZoneInfo.FindSystemTimeZoneById(timeZoneId));
});

var app = builder.Build();

// Created now, so an unknown App:TimeZone stops the start-up instead of failing the first request.
app.Services.GetRequiredService<ConfiguredTimeZoneCalendar>();

using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<GrantTrackerDbContext>().Database;
    if (app.Environment.IsDevelopment())
    {
        database.EnsureDeleted(); // seed dates are relative to today, so Development starts fresh
    }

    database.EnsureCreated(); // other environments keep their data across restarts
}

// Swagger only in Development: it is a tool for developers and reviewers, not for officers.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();    // http://localhost:5080/swagger/v1/swagger.json
    app.UseSwaggerUI();  // http://localhost:5080/swagger
}

// Endpoints are mapped here in later steps.

app.Run();

// Lets the integration tests start the API with WebApplicationFactory<Program>.
public partial class Program;
