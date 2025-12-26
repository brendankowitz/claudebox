namespace ClaudeBox.Models;

/// <summary>
/// Represents the usage data from Claude CLI.
/// </summary>
public class ClaudeUsage
{
    /// <summary>
    /// Five-hour window usage information.
    /// </summary>
    public UsageWindow? FiveHour { get; set; }

    /// <summary>
    /// Seven-day (weekly) window usage information.
    /// </summary>
    public UsageWindow? SevenDay { get; set; }

    /// <summary>
    /// Seven-day Opus window usage information (if applicable).
    /// </summary>
    public UsageWindow? SevenDayOpus { get; set; }

    /// <summary>
    /// The subscription plan type (e.g., "pro", "max").
    /// </summary>
    public string? Plan { get; set; }

    /// <summary>
    /// User email if available.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Organization name if available.
    /// </summary>
    public string? Organization { get; set; }

    /// <summary>
    /// Timestamp when this data was fetched.
    /// </summary>
    public DateTime FetchedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Error message if data fetch failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Indicates if the data is valid.
    /// </summary>
    public bool IsValid => string.IsNullOrEmpty(Error) && (FiveHour != null || SevenDay != null);
}

/// <summary>
/// Represents a usage window (e.g., 5-hour, 7-day).
/// </summary>
public class UsageWindow
{
    /// <summary>
    /// Usage utilization as a percentage (0-100).
    /// </summary>
    public double Utilization { get; set; }

    /// <summary>
    /// When this usage window resets.
    /// </summary>
    public DateTime? ResetsAt { get; set; }

    /// <summary>
    /// Human-readable reset time string.
    /// </summary>
    public string? ResetString { get; set; }

    /// <summary>
    /// The percentage remaining.
    /// </summary>
    public double Remaining => Math.Max(0, 100 - Utilization);

    /// <summary>
    /// Gets a formatted string showing time until reset.
    /// </summary>
    public string TimeUntilReset
    {
        get
        {
            if (ResetsAt == null)
                return ResetString ?? "Unknown";

            var remaining = ResetsAt.Value - DateTime.UtcNow;
            if (remaining.TotalSeconds <= 0)
                return "Resetting soon...";

            if (remaining.TotalDays >= 1)
                return $"{(int)remaining.TotalDays}d {remaining.Hours}h";
            if (remaining.TotalHours >= 1)
                return $"{(int)remaining.TotalHours}h {remaining.Minutes}m";
            return $"{remaining.Minutes}m";
        }
    }
}
