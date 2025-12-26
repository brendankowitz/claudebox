using System.Windows;
using System.Windows.Interop;
using ClaudeBox.ViewModels;

namespace ClaudeBox.Views;

/// <summary>
/// Popup window that appears near the system tray.
/// </summary>
public partial class QuotaPopupWindow : Window
{
    private readonly MainViewModel _viewModel;

    public QuotaPopupWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        PopupContent.DataContext = viewModel;

        Loaded += QuotaPopupWindow_Loaded;
    }

    private void QuotaPopupWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Position window near system tray
        PositionNearTray();
    }

    private void PositionNearTray()
    {
        // Get the working area and position near the bottom-right corner (where the tray is)
        var workingArea = SystemParameters.WorkArea;
        
        Left = workingArea.Right - ActualWidth - 10;
        Top = workingArea.Bottom - ActualHeight - 10;
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        // Close when window loses focus
        _viewModel.IsPopupOpen = false;
        Close();
    }
}
