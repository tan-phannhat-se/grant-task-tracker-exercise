using System.Text.Json.Serialization;
using GrantTracker.Api.Data;
using GrantTracker.Api.Endpoints;
using GrantTracker.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI description + Swagger UI, so the endpoints can be tried from the browser.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<GrantTrackerDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("GrantTracker")));
builder.Services.AddScoped<GrantService>();

// Enums go out by name ("Open"), so clients do not depend on the stored numbers.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// One clock for the whole app; the tests replace it with a fake one.
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddSingleton(services =>
{
    var timeZoneId = services.GetRequiredService<IConfiguration>()["App:TimeZone"] ?? "UTC";
    return new ConfiguredTimeZoneCalendar(
        services.GetRequiredService<TimeProvider>(),
        TimeZoneInfo.FindSystemTimeZoneById(timeZoneId));
});

// Errors as ProblemDetails: a broken business rule → 400 or 409 with its code; anything else → 500.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainRuleExceptionHandler>();

// A body that cannot be read gets a 400 in every environment.
// Development would otherwise throw it, and the exception handler would turn it into a 500.
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);

var app = builder.Build();

// Created now, so an unknown App:TimeZone stops the start-up instead of failing the first request.
var calendar = app.Services.GetRequiredService<ConfiguredTimeZoneCalendar>();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GrantTrackerDbContext>();
    if (app.Environment.IsDevelopment())
    {
        // Seed dates are relative to today, so Development starts fresh every time.
        // The sample data is fictional, so no other environment gets it.
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
        SeedData.Seed(db, calendar.Today());
    }
    else
    {
        db.Database.EnsureCreated(); // keeps existing data across restarts
    }
}

app.UseExceptionHandler();

// Swagger only in Development: it is a tool for developers and reviewers, not for officers.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();    // http://localhost:5080/swagger/v1/swagger.json
    app.UseSwaggerUI();  // http://localhost:5080/swagger
}

app.MapGrantEndpoints();

app.Run();

// Lets the integration tests start the API with WebApplicationFactory<Program>.
public partial class Program;
