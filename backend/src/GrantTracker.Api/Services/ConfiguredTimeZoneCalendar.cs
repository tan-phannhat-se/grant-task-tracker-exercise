namespace GrantTracker.Api.Services;

/// <summary>
/// Gives "today" as the date in the configured time zone.
/// </summary>
public class ConfiguredTimeZoneCalendar
{
    /// <summary>
    /// Creates a calendar that reads the time from the given clock.
    /// </summary>
    public ConfiguredTimeZoneCalendar(TimeProvider timeProvider, TimeZoneInfo timeZone)
    {
    }

    /// <summary>
    /// Today's date in the configured time zone.
    /// </summary>
    public DateOnly Today() => throw new NotImplementedException();
}
