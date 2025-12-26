using System.Windows;
using ClaudeBox.ViewModels;
using ClaudeBox.Views;
using H.NotifyIcon;

namespace ClaudeBox;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private TaskbarIcon? _taskbarIcon;
    private MainViewModel? _viewModel;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _viewModel = new MainViewModel();
        
        // Create the taskbar icon
        _taskbarIcon = new TaskbarIcon
        {
            ToolTipText = _viewModel.TooltipText,
            ContextMenu = CreateContextMenu(),
            LeftClickCommand = _viewModel.TogglePopupCommand
        };

        // Update icon when view model changes
        _viewModel.PropertyChanged += (s, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.TrayIcon) && _viewModel.TrayIcon != null)
            {
                _taskbarIcon.Icon = _viewModel.TrayIcon;
            }
            else if (args.PropertyName == nameof(MainViewModel.TooltipText))
            {
                _taskbarIcon.ToolTipText = _viewModel.TooltipText;
            }
            else if (args.PropertyName == nameof(MainViewModel.IsPopupOpen))
            {
                if (_viewModel.IsPopupOpen)
                {
                    ShowPopup();
                }
            }
        };

        // Set initial icon
        if (_viewModel.TrayIcon != null)
        {
            _taskbarIcon.Icon = _viewModel.TrayIcon;
        }
    }

    private System.Windows.Controls.ContextMenu CreateContextMenu()
    {
        var menu = new System.Windows.Controls.ContextMenu();
        menu.Style = CreateContextMenuStyle();

        // Refresh
        var refreshItem = new System.Windows.Controls.MenuItem { Header = "Refresh Now" };
        refreshItem.Click += async (s, e) => await _viewModel!.RefreshCommand.ExecuteAsync(null);
        menu.Items.Add(refreshItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        // Refresh interval submenu
        var intervalMenu = new System.Windows.Controls.MenuItem { Header = "Refresh Interval" };
        foreach (var interval in new[] { 1, 2, 5, 15, 30 })
        {
            var item = new System.Windows.Controls.MenuItem
            {
                Header = $"{interval} minute{(interval > 1 ? "s" : "")}",
                Tag = interval
            };
            item.Click += (s, e) =>
            {
                _viewModel!.RefreshIntervalMinutes = interval;
                UpdateIntervalMenuCheckmarks(intervalMenu);
            };
            intervalMenu.Items.Add(item);
        }
        UpdateIntervalMenuCheckmarks(intervalMenu);
        menu.Items.Add(intervalMenu);

        menu.Items.Add(new System.Windows.Controls.Separator());

        // Settings
        var settingsItem = new System.Windows.Controls.MenuItem { Header = "Settings..." };
        settingsItem.Click += (s, e) => _viewModel!.OpenSettingsCommand.Execute(null);
        menu.Items.Add(settingsItem);

        // About
        var aboutItem = new System.Windows.Controls.MenuItem { Header = "About ClaudeBox" };
        aboutItem.Click += (s, e) => _viewModel!.ShowAboutCommand.Execute(null);
        menu.Items.Add(aboutItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        // Exit
        var exitItem = new System.Windows.Controls.MenuItem { Header = "Exit" };
        exitItem.Click += (s, e) => _viewModel!.ExitCommand.Execute(null);
        menu.Items.Add(exitItem);

        return menu;
    }

    private void UpdateIntervalMenuCheckmarks(System.Windows.Controls.MenuItem intervalMenu)
    {
        foreach (System.Windows.Controls.MenuItem item in intervalMenu.Items)
        {
            if (item.Tag is int interval)
            {
                item.IsChecked = interval == _viewModel!.RefreshIntervalMinutes;
            }
        }
    }

    private static System.Windows.Style CreateContextMenuStyle()
    {
        var style = new System.Windows.Style(typeof(System.Windows.Controls.ContextMenu));
        style.Setters.Add(new Setter(System.Windows.Controls.Control.BackgroundProperty,
            new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30))));
        style.Setters.Add(new Setter(System.Windows.Controls.Control.ForegroundProperty,
            new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White)));
        return style;
    }

    private void ShowPopup()
    {
        var popup = new QuotaPopupWindow(_viewModel!);
        popup.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _taskbarIcon?.Dispose();
        _viewModel?.Dispose();
        base.OnExit(e);
    }
}

