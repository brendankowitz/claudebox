using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClaudeBox.Models;
using ClaudeBox.Services;
using System.Drawing;
using H.NotifyIcon;

namespace ClaudeBox.ViewModels;

/// <summary>
/// Main view model for the ClaudeBox system tray application.
/// </summary>
public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly ClaudeCliService _claudeService;
    private readonly IconGeneratorService _iconGenerator;
    private readonly DispatcherTimer _refreshTimer;
    private bool _disposed;

    [ObservableProperty]
    private ClaudeUsage? _currentUsage;

    [ObservableProperty]
    private Icon? _trayIcon;

    [ObservableProperty]
    private string _tooltipText = "ClaudeBox - Loading...";

    [ObservableProperty]
    private bool _isPopupOpen;

    [ObservableProperty]
    private string _statusText = "Initializing...";

    [ObservableProperty]
    private int _refreshIntervalMinutes = 5;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private double _hourlyUsage;

    [ObservableProperty]
    private double _weeklyUsage;

    [ObservableProperty]
    private string _hourlyResetText = "";

    [ObservableProperty]
    private string _weeklyResetText = "";

    [ObservableProperty]
    private string _planText = "";

    [ObservableProperty]
    private string _accountText = "";

    [ObservableProperty]
    private bool _cliAvailable;

    [ObservableProperty]
    private string _cliStatusText = "";

    public MainViewModel()
    {
        _claudeService = new ClaudeCliService();
        _iconGenerator = new IconGeneratorService();
        
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(RefreshIntervalMinutes)
        };
        _refreshTimer.Tick += async (s, e) => await RefreshAsync();

        // Initialize asynchronously but fire and forget from constructor
        // This is acceptable for UI initialization
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        // Generate default icons if they don't exist
        EnsureIconsExist();

        // Set initial icon
        TrayIcon = _iconGenerator.GenerateProgressIcon(0, null, true);

        // Check CLI availability
        CliAvailable = _claudeService.IsCliAvailable;
        if (!CliAvailable)
        {
            CliStatusText = "Claude CLI not found. Please install Claude CLI.";
            StatusText = "CLI not available";
            TooltipText = "ClaudeBox - Claude CLI not found";
        }
        else
        {
            CliStatusText = $"Claude CLI found: {_claudeService.CliPath}";
            
            // Initial fetch
            await RefreshAsync();
            
            // Start auto-refresh
            _refreshTimer.Start();
        }
    }

    private void EnsureIconsExist()
    {
        try
        {
            var resourcesPath = Path.Combine(AppContext.BaseDirectory, "Resources");
            if (!Directory.Exists(resourcesPath))
            {
                Directory.CreateDirectory(resourcesPath);
            }

            var mainIconPath = Path.Combine(resourcesPath, "claudebox.ico");
            var grayIconPath = Path.Combine(resourcesPath, "claudebox-gray.ico");

            if (!File.Exists(mainIconPath) || !File.Exists(grayIconPath))
            {
                _iconGenerator.CreateDefaultIcons(resourcesPath);
            }
        }
        catch
        {
            // Ignore icon creation errors - we'll generate them dynamically
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsRefreshing || !CliAvailable)
            return;

        try
        {
            IsRefreshing = true;
            StatusText = "Refreshing...";

            CurrentUsage = await _claudeService.GetUsageAsync(forceRefresh: true);

            if (CurrentUsage.IsValid)
            {
                UpdateFromUsage(CurrentUsage);
                StatusText = $"Last updated: {CurrentUsage.FetchedAt:HH:mm:ss}";
            }
            else
            {
                StatusText = CurrentUsage.Error ?? "Unknown error";
                TrayIcon = _iconGenerator.GenerateProgressIcon(0, null, true);
                TooltipText = $"ClaudeBox - Error: {CurrentUsage.Error}";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
            TrayIcon = _iconGenerator.GenerateProgressIcon(0, null, true);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private void UpdateFromUsage(ClaudeUsage usage)
    {
        // Update hourly (5-hour) usage
        if (usage.FiveHour != null)
        {
            HourlyUsage = usage.FiveHour.Utilization;
            HourlyResetText = $"Resets in {usage.FiveHour.TimeUntilReset}";
        }
        else
        {
            HourlyUsage = 0;
            HourlyResetText = "No data";
        }

        // Update weekly (7-day) usage
        if (usage.SevenDay != null)
        {
            WeeklyUsage = usage.SevenDay.Utilization;
            WeeklyResetText = $"Resets in {usage.SevenDay.TimeUntilReset}";
        }
        else
        {
            WeeklyUsage = 0;
            WeeklyResetText = "No data";
        }

        // Update plan and account info
        PlanText = !string.IsNullOrEmpty(usage.Plan) ? $"Plan: {usage.Plan}" : "";
        AccountText = !string.IsNullOrEmpty(usage.Email) ? usage.Email : 
                      !string.IsNullOrEmpty(usage.Organization) ? usage.Organization : "";

        // Update tray icon - show weekly usage in the icon
        TrayIcon = _iconGenerator.GenerateProgressIcon(
            WeeklyUsage, 
            usage.FiveHour != null ? HourlyUsage : null);

        // Update tooltip
        var hourlyRemaining = 100 - HourlyUsage;
        var weeklyRemaining = 100 - WeeklyUsage;
        TooltipText = $"ClaudeBox - 5h: {hourlyRemaining:F0}% | Weekly: {weeklyRemaining:F0}%";
    }

    partial void OnRefreshIntervalMinutesChanged(int value)
    {
        if (value > 0)
        {
            _refreshTimer.Interval = TimeSpan.FromMinutes(value);
        }
    }

    [RelayCommand]
    private void SetRefreshInterval(int minutes)
    {
        RefreshIntervalMinutes = minutes;
    }

    [RelayCommand]
    private void TogglePopup()
    {
        IsPopupOpen = !IsPopupOpen;
    }

    [RelayCommand]
    private void OpenSettings()
    {
        // Open settings window
        var settingsWindow = new Views.SettingsWindow(this);
        settingsWindow.ShowDialog();
    }

    [RelayCommand]
    private void Exit()
    {
        _refreshTimer.Stop();
        Application.Current.Shutdown();
    }

    [RelayCommand]
    private void ShowAbout()
    {
        MessageBox.Show(
            "ClaudeBox v1.0.0\n\n" +
            "A Windows system tray application that displays your Claude quota usage.\n\n" +
            "Similar to CodexBar for macOS, but for Windows.\n\n" +
            "GitHub: github.com/brendankowitz/claudebox",
            "About ClaudeBox",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _refreshTimer.Stop();
        TrayIcon?.Dispose();
        GC.SuppressFinalize(this);
    }
}
