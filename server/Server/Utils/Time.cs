namespace server.Utils;

public static class Time
{
    /// <summary>
    /// Converts DateTime to UTC if it has Kind=Unspecified, otherwise returns as-is.
    /// This handles the PostgreSQL timezone requirement.
    /// </summary>
    /// <param name="dateTime">The DateTime to convert.</param>
    /// <returns>The converted DateTime</returns>
    public static DateTime? ConvertToUtcIfSpecified(DateTime dateTime)
    {    
        // If Kind is Unspecified, treat it as UTC (common for date-only inputs)
        if (dateTime.Kind == DateTimeKind.Unspecified)
        {
            return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        }
        
        // If it's already UTC, return as-is
        if (dateTime.Kind == DateTimeKind.Utc)
        {
            return dateTime;
        }
        
        // If it's Local, convert to UTC
        return dateTime.ToUniversalTime();
    }
}
