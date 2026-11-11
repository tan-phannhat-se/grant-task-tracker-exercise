using GrantTracker.Api.Services;
using Microsoft.Extensions.Time.Testing;

namespace GrantTracker.Tests.Services;

/// <summary>
/// "Today" is the date in the configured time zone, taken from the injected clock.
/// </summary>
public class ConfiguredTimeZoneCalendarTests
{
    // 15 Jan 23:30 in UTC is already 16 Jan 10:30 in Sydney (UTC+11 in summer).
    // A past date, so a calendar that reads the real clock can never return the expected day.
    private static readonly DateTimeOffset LateEveningUtc = new(2020, 1, 15, 23, 30, 0, TimeSpan.Zero);

    /// <summary>
    /// CL-1: with UTC configured, 2020-01-15 23:30Z → today is 15 Jan.
    /// Catches: the real clock used instead of the injected one, or a fixed time zone ignoring the setting.
    /// </summary>
    [Fact]
    public void Today_UtcConfigured_ReturnsTheUtcDate()
    {
        var calendar = new ConfiguredTimeZoneCalendar(new FakeTimeProvider(LateEveningUtc), TimeZoneInfo.Utc);

        var today = calendar.Today();

        Assert.Equal(new DateOnly(2020, 1, 15), today);
    }

    /// <summary>
    /// CL-2: with Australia/Sydney configured, the same instant → today is 16 Jan.
    /// Catches: the configured time zone ignored, so the UTC date is returned.
    /// </summary>
    [Fact]
    public void Today_SydneyConfigured_ReturnsTheSydneyDate()
    {
        var sydney = TimeZoneInfo.FindSystemTimeZoneById("Australia/Sydney");
        var calendar = new ConfiguredTimeZoneCalendar(new FakeTimeProvider(LateEveningUtc), sydney);

        var today = calendar.Today();

        Assert.Equal(new DateOnly(2020, 1, 16), today);
    }
}
