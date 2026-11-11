using GrantTracker.Api.Services;
using Microsoft.Extensions.Time.Testing;

namespace GrantTracker.Tests.Services;

/// <summary>
/// "Today" is the date in the configured time zone, taken from the injected clock.
/// </summary>
public class ConfiguredTimeZoneCalendarTests
{
    // 11 Nov 23:30 in UTC is already 12 Nov 10:30 in Sydney (UTC+11 in November).
    private static readonly DateTimeOffset LateEveningUtc = new(2026, 11, 11, 23, 30, 0, TimeSpan.Zero);

    /// <summary>
    /// CL-1: with UTC configured, 2026-11-11 23:30Z → today is 11 Nov.
    /// Catches: the real clock used instead of the injected one, or a fixed time zone ignoring the setting.
    /// </summary>
    [Fact]
    public void Today_UtcConfigured_ReturnsTheUtcDate()
    {
        var calendar = new ConfiguredTimeZoneCalendar(new FakeTimeProvider(LateEveningUtc), TimeZoneInfo.Utc);

        var today = calendar.Today();

        Assert.Equal(new DateOnly(2026, 11, 11), today);
    }

    /// <summary>
    /// CL-2: with Australia/Sydney configured, the same instant → today is 12 Nov.
    /// Catches: the configured time zone ignored, so the UTC date is returned.
    /// </summary>
    [Fact]
    public void Today_SydneyConfigured_ReturnsTheSydneyDate()
    {
        var sydney = TimeZoneInfo.FindSystemTimeZoneById("Australia/Sydney");
        var calendar = new ConfiguredTimeZoneCalendar(new FakeTimeProvider(LateEveningUtc), sydney);

        var today = calendar.Today();

        Assert.Equal(new DateOnly(2026, 11, 12), today);
    }
}
