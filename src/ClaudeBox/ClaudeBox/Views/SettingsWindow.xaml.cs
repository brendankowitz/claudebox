using System.Windows;
using System.Windows.Controls;
using ClaudeBox.ViewModels;

namespace ClaudeBox.Views;

/// <summary>
/// Settings window for ClaudeBox.
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly MainViewModel _viewModel;

    public SettingsWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;

        // Set current values
        CliStatusTextBlock.Text = _viewModel.CliStatusText;

        // Select current refresh interval
        var currentInterval = _viewModel.RefreshIntervalMinutes;
        for (int i = 0; i < RefreshIntervalCombo.Items.Count; i++)
        {
            var item = (ComboBoxItem)RefreshIntervalCombo.Items[i];
            if (int.TryParse(item.Tag?.ToString(), out int tag) && tag == currentInterval)
            {
                RefreshIntervalCombo.SelectedIndex = i;
                break;
            }
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        // Save refresh interval
        var selectedItem = (ComboBoxItem)RefreshIntervalCombo.SelectedItem;
        if (selectedItem != null && int.TryParse(selectedItem.Tag?.ToString(), out int interval))
        {
            _viewModel.RefreshIntervalMinutes = interval;
        }

        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
