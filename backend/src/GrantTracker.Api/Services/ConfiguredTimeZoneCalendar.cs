namespace GrantTracker.Api.Services;

/// <summary>
/// Gives "today" as the date in the configured time zone.
/// </summary>
public class ConfiguredTimeZoneCalendar
{
    private readonly TimeProvider _timeProvider;
    private readonly TimeZoneInfo _timeZone;

    /// <summary>
    /// Creates a calendar that reads the time from the given clock.
    /// </summary>
    public ConfiguredTimeZoneCalendar(TimeProvider timeProvider, TimeZoneInfo timeZone)
    {
        _timeProvider = timeProvider;
        _timeZone = timeZone;
    }

    /// <summary>
    /// Today's date in the configured time zone.
    /// </summary>
    public DateOnly Today()
    {
        var nowInTimeZone = TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), _timeZone);
        return DateOnly.FromDateTime(nowInTimeZone.DateTime);
    }
}
