using ClaudeBox.Models;

namespace ClaudeBox.Tests;

public class ClaudeUsageTests
{
    [Fact]
    public void ClaudeUsage_IsValid_ReturnsFalse_WhenError()
    {
        var usage = new ClaudeUsage
        {
            Error = "Some error"
        };

        Assert.False(usage.IsValid);
    }

    [Fact]
    public void ClaudeUsage_IsValid_ReturnsFalse_WhenNoData()
    {
        var usage = new ClaudeUsage();

        Assert.False(usage.IsValid);
    }

    [Fact]
    public void ClaudeUsage_IsValid_ReturnsTrue_WithFiveHourData()
    {
        var usage = new ClaudeUsage
        {
            FiveHour = new UsageWindow { Utilization = 50 }
        };

        Assert.True(usage.IsValid);
    }

    [Fact]
    public void ClaudeUsage_IsValid_ReturnsTrue_WithSevenDayData()
    {
        var usage = new ClaudeUsage
        {
            SevenDay = new UsageWindow { Utilization = 30 }
        };

        Assert.True(usage.IsValid);
    }
}

public class UsageWindowTests
{
    [Theory]
    [InlineData(0, 100)]
    [InlineData(50, 50)]
    [InlineData(100, 0)]
    [InlineData(75.5, 24.5)]
    public void UsageWindow_Remaining_CalculatesCorrectly(double utilization, double expectedRemaining)
    {
        var window = new UsageWindow { Utilization = utilization };

        Assert.Equal(expectedRemaining, window.Remaining);
    }

    [Fact]
    public void UsageWindow_TimeUntilReset_ReturnsUnknown_WhenNoResetTime()
    {
        var window = new UsageWindow();

        Assert.Equal("Unknown", window.TimeUntilReset);
    }

    [Fact]
    public void UsageWindow_TimeUntilReset_ReturnsResetString_WhenNoResetsAt()
    {
        var window = new UsageWindow { ResetString = "in 2 hours" };

        Assert.Equal("in 2 hours", window.TimeUntilReset);
    }

    [Fact]
    public void UsageWindow_TimeUntilReset_ReturnsFormattedTime_WhenFutureDate()
    {
        var window = new UsageWindow 
        { 
            ResetsAt = DateTime.UtcNow.AddHours(2).AddMinutes(30) 
        };

        var result = window.TimeUntilReset;
        
        // Should contain hours and minutes
        Assert.Contains("h", result);
        Assert.Contains("m", result);
    }

    [Fact]
    public void UsageWindow_TimeUntilReset_ReturnsDays_WhenMoreThanOneDay()
    {
        var window = new UsageWindow 
        { 
            ResetsAt = DateTime.UtcNow.AddDays(2).AddHours(3) 
        };

        var result = window.TimeUntilReset;
        
        // Should contain days
        Assert.Contains("d", result);
    }

    [Fact]
    public void UsageWindow_TimeUntilReset_ReturnsResettingSoon_WhenPastDate()
    {
        var window = new UsageWindow 
        { 
            ResetsAt = DateTime.UtcNow.AddMinutes(-5) 
        };

        Assert.Equal("Resetting soon...", window.TimeUntilReset);
    }
}
