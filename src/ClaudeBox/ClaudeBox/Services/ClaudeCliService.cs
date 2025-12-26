using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClaudeBox.Models;

namespace ClaudeBox.Services;

/// <summary>
/// Service for interacting with the Claude CLI to fetch usage data.
/// </summary>
public partial class ClaudeCliService
{
    private readonly string _claudePath;
    private ClaudeUsage? _cachedUsage;
    private DateTime _lastFetch = DateTime.MinValue;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromSeconds(30);

    public ClaudeCliService()
    {
        _claudePath = FindClaudeCli();
    }

    /// <summary>
    /// Gets the cached usage data or fetches new data if cache is expired.
    /// </summary>
    public async Task<ClaudeUsage> GetUsageAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && _cachedUsage != null && DateTime.Now - _lastFetch < _cacheExpiry)
        {
            return _cachedUsage;
        }

        try
        {
            _cachedUsage = await FetchUsageFromCliAsync();
            _lastFetch = DateTime.Now;
            return _cachedUsage;
        }
        catch (Exception ex)
        {
            return new ClaudeUsage
            {
                Error = ex.Message,
                FetchedAt = DateTime.Now
            };
        }
    }

    /// <summary>
    /// Checks if the Claude CLI is installed and available.
    /// </summary>
    public bool IsCliAvailable => !string.IsNullOrEmpty(_claudePath);

    /// <summary>
    /// Gets the path to the Claude CLI executable.
    /// </summary>
    public string CliPath => _claudePath;

    private static string FindClaudeCli()
    {
        var possiblePaths = new[]
        {
            "claude",
            "claude.cmd",
            "claude.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "claude", "claude.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "claude", "claude.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude", "local", "claude.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "Local", "Programs", "claude-code", "claude.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "npm", "claude.cmd"),
        };

        foreach (var path in possiblePaths)
        {
            if (path is "claude" or "claude.cmd" or "claude.exe")
            {
                // Try to find in PATH
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "where",
                        Arguments = path.Replace(".cmd", "").Replace(".exe", ""),
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    using var process = Process.Start(startInfo);
                    if (process != null)
                    {
                        var output = process.StandardOutput.ReadToEnd().Trim();
                        process.WaitForExit();
                        if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                        {
                            var firstPath = output.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                            if (!string.IsNullOrEmpty(firstPath) && File.Exists(firstPath))
                            {
                                return firstPath;
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore and try next path
                }
            }
            else if (File.Exists(path))
            {
                return path;
            }
        }

        return string.Empty;
    }

    private async Task<ClaudeUsage> FetchUsageFromCliAsync()
    {
        if (!IsCliAvailable)
        {
            return new ClaudeUsage
            {
                Error = "Claude CLI not found. Please install Claude CLI and ensure it's in your PATH.",
                FetchedAt = DateTime.Now
            };
        }

        var usage = new ClaudeUsage { FetchedAt = DateTime.Now };

        try
        {
            // Run claude with /usage command
            var usageOutput = await RunClaudeCommandAsync("/usage");
            ParseUsageOutput(usageOutput, usage);

            // Run claude with /status to get additional info
            var statusOutput = await RunClaudeCommandAsync("/status");
            ParseStatusOutput(statusOutput, usage);
        }
        catch (Exception ex)
        {
            usage.Error = $"Failed to fetch usage data: {ex.Message}";
        }

        return usage;
    }

    private async Task<string> RunClaudeCommandAsync(string command)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _claudePath,
            Arguments = command,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        
        await process.WaitForExitAsync();

        if (!string.IsNullOrEmpty(error) && process.ExitCode != 0)
        {
            throw new Exception(error);
        }

        return output;
    }

    private static void ParseUsageOutput(string output, ClaudeUsage usage)
    {
        if (string.IsNullOrWhiteSpace(output))
            return;

        // Try to parse as JSON first (newer CLI versions)
        try
        {
            using var doc = JsonDocument.Parse(output);
            var root = doc.RootElement;

            if (root.TryGetProperty("five_hour", out var fiveHour))
            {
                usage.FiveHour = ParseUsageWindow(fiveHour);
            }

            if (root.TryGetProperty("seven_day", out var sevenDay))
            {
                usage.SevenDay = ParseUsageWindow(sevenDay);
            }

            if (root.TryGetProperty("seven_day_opus", out var sevenDayOpus))
            {
                usage.SevenDayOpus = ParseUsageWindow(sevenDayOpus);
            }

            if (root.TryGetProperty("plan", out var plan))
            {
                usage.Plan = plan.GetString();
            }

            return;
        }
        catch (JsonException)
        {
            // Not JSON, try text parsing
        }

        // Text parsing for older CLI versions or different output formats
        ParseUsageText(output, usage);
    }

    private static UsageWindow? ParseUsageWindow(JsonElement element)
    {
        var window = new UsageWindow();

        if (element.TryGetProperty("utilization", out var utilization))
        {
            window.Utilization = utilization.GetDouble();
        }

        if (element.TryGetProperty("resets_at", out var resetsAt))
        {
            var resetStr = resetsAt.GetString();
            if (!string.IsNullOrEmpty(resetStr) && DateTime.TryParse(resetStr, out var resetTime))
            {
                window.ResetsAt = resetTime.ToUniversalTime();
            }
            window.ResetString = resetStr;
        }

        return window;
    }

    private static void ParseUsageText(string output, ClaudeUsage usage)
    {
        // Parse text output like:
        // "5-hour usage: 6.0% (resets at 2025-11-04T04:59:59)"
        // "7-day usage: 35.0% (resets at 2025-11-06T03:59:59)"
        
        var fiveHourMatch = FiveHourUsageRegex().Match(output);
        if (fiveHourMatch.Success)
        {
            usage.FiveHour = new UsageWindow
            {
                Utilization = double.Parse(fiveHourMatch.Groups[1].Value),
                ResetString = fiveHourMatch.Groups.Count > 2 ? fiveHourMatch.Groups[2].Value : null
            };

            if (!string.IsNullOrEmpty(usage.FiveHour.ResetString) && 
                DateTime.TryParse(usage.FiveHour.ResetString, out var resetTime))
            {
                usage.FiveHour.ResetsAt = resetTime.ToUniversalTime();
            }
        }

        var sevenDayMatch = SevenDayUsageRegex().Match(output);
        if (sevenDayMatch.Success)
        {
            usage.SevenDay = new UsageWindow
            {
                Utilization = double.Parse(sevenDayMatch.Groups[1].Value),
                ResetString = sevenDayMatch.Groups.Count > 2 ? sevenDayMatch.Groups[2].Value : null
            };

            if (!string.IsNullOrEmpty(usage.SevenDay.ResetString) && 
                DateTime.TryParse(usage.SevenDay.ResetString, out var resetTime))
            {
                usage.SevenDay.ResetsAt = resetTime.ToUniversalTime();
            }
        }

        // Try to extract percentages from any format
        if (usage.FiveHour == null && usage.SevenDay == null)
        {
            var percentMatches = PercentageRegex().Matches(output);
            if (percentMatches.Count >= 1)
            {
                usage.FiveHour = new UsageWindow { Utilization = double.Parse(percentMatches[0].Groups[1].Value) };
            }
            if (percentMatches.Count >= 2)
            {
                usage.SevenDay = new UsageWindow { Utilization = double.Parse(percentMatches[1].Groups[1].Value) };
            }
        }
    }

    private static void ParseStatusOutput(string output, ClaudeUsage usage)
    {
        if (string.IsNullOrWhiteSpace(output))
            return;

        // Try JSON parsing first
        try
        {
            using var doc = JsonDocument.Parse(output);
            var root = doc.RootElement;

            if (root.TryGetProperty("email", out var email))
            {
                usage.Email = email.GetString();
            }

            if (root.TryGetProperty("organization", out var org))
            {
                usage.Organization = org.GetString();
            }

            if (root.TryGetProperty("plan", out var plan))
            {
                usage.Plan = plan.GetString();
            }

            return;
        }
        catch (JsonException)
        {
            // Not JSON, try text parsing
        }

        // Text parsing
        var emailMatch = EmailRegex().Match(output);
        if (emailMatch.Success)
        {
            usage.Email = emailMatch.Groups[1].Value;
        }

        var planMatch = PlanRegex().Match(output);
        if (planMatch.Success)
        {
            usage.Plan = planMatch.Groups[1].Value;
        }

        var orgMatch = OrgRegex().Match(output);
        if (orgMatch.Success)
        {
            usage.Organization = orgMatch.Groups[1].Value;
        }
    }

    [GeneratedRegex(@"5[- ]?hour.*?(\d+\.?\d*)%(?:.*?(?:resets?\s*(?:at\s*)?)?([^\n]+))?", RegexOptions.IgnoreCase)]
    private static partial Regex FiveHourUsageRegex();

    [GeneratedRegex(@"(?:7[- ]?day|week).*?(\d+\.?\d*)%(?:.*?(?:resets?\s*(?:at\s*)?)?([^\n]+))?", RegexOptions.IgnoreCase)]
    private static partial Regex SevenDayUsageRegex();

    [GeneratedRegex(@"(\d+\.?\d*)%")]
    private static partial Regex PercentageRegex();

    [GeneratedRegex(@"(?:email|user):\s*(.+)", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"(?:plan|subscription):\s*(.+)", RegexOptions.IgnoreCase)]
    private static partial Regex PlanRegex();

    [GeneratedRegex(@"(?:org|organization):\s*(.+)", RegexOptions.IgnoreCase)]
    private static partial Regex OrgRegex();
}
