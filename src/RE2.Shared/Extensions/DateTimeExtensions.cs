namespace RE2.Shared.Extensions;

/// <summary>
/// Extension methods for DateTime and DateOnly types.
/// Provides utility methods for licence expiry calculations, alert generation, and date comparisons.
/// </summary>
public static class DateTimeExtensions
{
    #region Expiry and Validity Checks

    /// <summary>
    /// Checks if a date has expired (is in the past).
    /// </summary>
    /// <param name="date">The date to check.</param>
    /// <param name="asOfDate">The reference date (default: today).</param>
    /// <returns>True if the date is before the reference date.</returns>
    public static bool IsExpired(this DateOnly date, DateOnly? asOfDate = null)
    {
        var referenceDate = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return date < referenceDate;
    }

    /// <summary>
    /// Checks if a date has expired (is in the past).
    /// </summary>
    /// <param name="date">The date to check.</param>
    /// <param name="asOfDate">The reference date (default: now).</param>
    /// <returns>True if the date is before the reference date.</returns>
    public static bool IsExpired(this DateTime date, DateTime? asOfDate = null)
    {
        var referenceDate = asOfDate ?? DateTime.UtcNow;
        return date < referenceDate;
    }

    /// <summary>
    /// Checks if a date is valid (not expired).
    /// </summary>
    /// <param name="date">The date to check.</param>
    /// <param name="asOfDate">The reference date (default: today).</param>
    /// <returns>True if the date is today or in the future.</returns>
    public static bool IsValid(this DateOnly date, DateOnly? asOfDate = null)
    {
        return !date.IsExpired(asOfDate);
    }

    /// <summary>
    /// Checks if a nullable expiry date is valid (null means no expiry, never expires).
    /// </summary>
    /// <param name="expiryDate">The nullable expiry date.</param>
    /// <param name="asOfDate">The reference date (default: today).</param>
    /// <returns>True if the date is null (no expiry) or in the future.</returns>
    public static bool IsValidOrNoExpiry(this DateOnly? expiryDate, DateOnly? asOfDate = null)
    {
        if (!expiryDate.HasValue)
        {
            return true; // No expiry date means never expires
        }

        return expiryDate.Value.IsValid(asOfDate);
    }

    #endregion

    #region Expiry Warning Calculations

    /// <summary>
    /// Calculates the number of days until expiry.
    /// </summary>
    /// <param name="expiryDate">The expiry date.</param>
    /// <param name="asOfDate">The reference date (default: today).</param>
    /// <returns>Number of days until expiry (negative if already expired).</returns>
    public static int DaysUntilExpiry(this DateOnly expiryDate, DateOnly? asOfDate = null)
    {
        var referenceDate = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return expiryDate.DayNumber - referenceDate.DayNumber;
    }

    /// <summary>
    /// Checks if a date is expiring within a specified number of days.
    /// Used for alert generation (e.g., 90/60/30 day warnings per FR-007).
    /// </summary>
    /// <param name="expiryDate">The expiry date.</param>
    /// <param name="warningDays">Number of days before expiry to trigger warning.</param>
    /// <param name="asOfDate">The reference date (default: today).</param>
    /// <returns>True if the date is within the warning period.</returns>
    public static bool IsExpiringWithin(this DateOnly expiryDate, int warningDays, DateOnly? asOfDate = null)
    {
        var daysUntil = expiryDate.DaysUntilExpiry(asOfDate);
        return daysUntil >= 0 && daysUntil <= warningDays;
    }

    /// <summary>
    /// Gets the expiry warning level based on standard thresholds (90/60/30 days per FR-007).
    /// </summary>
    /// <param name="expiryDate">The expiry date.</param>
    /// <param name="asOfDate">The reference date (default: today).</param>
    /// <returns>Warning level: "Critical" (0-30 days), "Warning" (31-60 days), "Info" (61-90 days), or null if > 90 days.</returns>
    public static string? GetExpiryWarningLevel(this DateOnly expiryDate, DateOnly? asOfDate = null)
    {
        var daysUntil = expiryDate.DaysUntilExpiry(asOfDate);

        if (daysUntil < 0)
        {
            return "Expired";
        }

        if (daysUntil <= 30)
        {
            return "Critical";
        }

        if (daysUntil <= 60)
        {
            return "Warning";
        }

        if (daysUntil <= 90)
        {
            return "Info";
        }

        return null; // No warning needed
    }

    #endregion

    #region Date Arithmetic

    /// <summary>
    /// Adds months to a DateOnly value.
    /// </summary>
    /// <param name="date">The starting date.</param>
    /// <param name="months">Number of months to add.</param>
    /// <returns>The resulting date.</returns>
    public static DateOnly AddMonths(this DateOnly date, int months)
    {
        var dateTime = date.ToDateTime(TimeOnly.MinValue);
        var result = dateTime.AddMonths(months);
        return DateOnly.FromDateTime(result);
    }

    /// <summary>
    /// Adds years to a DateOnly value.
    /// </summary>
    /// <param name="date">The starting date.</param>
    /// <param name="years">Number of years to add.</param>
    /// <returns>The resulting date.</returns>
    public static DateOnly AddYears(this DateOnly date, int years)
    {
        var dateTime = date.ToDateTime(TimeOnly.MinValue);
        var result = dateTime.AddYears(years);
        return DateOnly.FromDateTime(result);
    }

    /// <summary>
    /// Calculates the expiry date based on a validity period in months.
    /// </summary>
    /// <param name="issueDate">The issue date.</param>
    /// <param name="validityMonths">Validity period in months.</param>
    /// <returns>The calculated expiry date.</returns>
    public static DateOnly CalculateExpiryDate(this DateOnly issueDate, int validityMonths)
    {
        return issueDate.AddMonths(validityMonths);
    }

    #endregion

    #region Formatting and Display

    /// <summary>
    /// Formats a DateOnly as ISO 8601 string (yyyy-MM-dd).
    /// </summary>
    /// <param name="date">The date to format.</param>
    /// <returns>ISO 8601 formatted string.</returns>
    public static string ToIso8601String(this DateOnly date)
    {
        return date.ToString("yyyy-MM-dd");
    }

    /// <summary>
    /// Formats a DateTime as ISO 8601 string with UTC timezone (yyyy-MM-ddTHH:mm:ssZ).
    /// </summary>
    /// <param name="dateTime">The datetime to format.</param>
    /// <returns>ISO 8601 formatted string with UTC indicator.</returns>
    public static string ToIso8601String(this DateTime dateTime)
    {
        return dateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
    }

    /// <summary>
    /// Formats a nullable DateOnly for display (returns "No Expiry" if null).
    /// </summary>
    /// <param name="date">The nullable date.</param>
    /// <param name="format">Format string (default: "yyyy-MM-dd").</param>
    /// <param name="nullText">Text to display if null (default: "No Expiry").</param>
    /// <returns>Formatted date string or null text.</returns>
    public static string ToDisplayString(this DateOnly? date, string format = "yyyy-MM-dd", string nullText = "No Expiry")
    {
        return date.HasValue ? date.Value.ToString(format) : nullText;
    }

    #endregion

    #region Conversion Helpers

    /// <summary>
    /// Converts DateTime to DateOnly (date portion only).
    /// </summary>
    /// <param name="dateTime">The DateTime to convert.</param>
    /// <returns>DateOnly representing the date portion.</returns>
    public static DateOnly ToDateOnly(this DateTime dateTime)
    {
        return DateOnly.FromDateTime(dateTime);
    }

    /// <summary>
    /// Converts DateOnly to DateTime at start of day (midnight UTC).
    /// </summary>
    /// <param name="date">The DateOnly to convert.</param>
    /// <returns>DateTime at midnight UTC.</returns>
    public static DateTime ToDateTimeUtc(this DateOnly date)
    {
        return date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
    }

    #endregion

    #region Validation Helpers

    /// <summary>
    /// Validates that an expiry date is after an issue date.
    /// </summary>
    /// <param name="expiryDate">The expiry date to validate.</param>
    /// <param name="issueDate">The issue date.</param>
    /// <returns>True if expiry date is after issue date.</returns>
    public static bool IsAfter(this DateOnly expiryDate, DateOnly issueDate)
    {
        return expiryDate > issueDate;
    }

    /// <summary>
    /// Validates that a date falls within a specified range.
    /// </summary>
    /// <param name="date">The date to validate.</param>
    /// <param name="startDate">Start of range (inclusive).</param>
    /// <param name="endDate">End of range (inclusive).</param>
    /// <returns>True if date is within range.</returns>
    public static bool IsBetween(this DateOnly date, DateOnly startDate, DateOnly endDate)
    {
        return date >= startDate && date <= endDate;
    }

    #endregion
}
