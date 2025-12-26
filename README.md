# ClaudeBox 📊

A Windows system tray application that displays your Claude quota usage, inspired by [CodexBar](https://github.com/steipete/CodexBar) for macOS.

## Features

- **System Tray Icon**: Shows a dynamic progress meter indicating your weekly Claude quota usage
- **Popup Dashboard**: Click the tray icon to see detailed progress bars for:
  - 5-hour window usage
  - Weekly (7-day) window usage
- **Auto-Refresh**: Configurable refresh intervals (1, 2, 5, 15, or 30 minutes)
- **Dark Theme**: Modern dark UI that matches the Claude aesthetic
- **Privacy-First**: All data stays local - reads usage directly from the Claude CLI

## Requirements

- Windows 10 or later
- .NET 8.0 Runtime
- [Claude CLI](https://code.claude.ai/) installed and logged in

## Installation

### Prerequisites

1. Install the Claude CLI following the instructions at [code.claude.ai](https://code.claude.ai/)
2. Log in to the Claude CLI:
   ```
   claude login
   ```
3. Run at least one command to generate usage data:
   ```
   claude /usage
   ```

### Running ClaudeBox

#### From Release
1. Download the latest release from the [Releases](https://github.com/brendankowitz/claudebox/releases) page
2. Extract and run `ClaudeBox.exe`

#### From Source
```bash
git clone https://github.com/brendankowitz/claudebox.git
cd claudebox
dotnet build src/ClaudeBox/ClaudeBox/ClaudeBox.csproj
dotnet run --project src/ClaudeBox/ClaudeBox/ClaudeBox.csproj
```

## Usage

1. After launching, ClaudeBox will appear in your system tray (notification area)
2. The tray icon shows a visual representation of your quota usage:
   - **Full circle**: Plenty of quota remaining
   - **Partial circle**: Some quota used
   - **Warning colors**: Yellow when >75% used, Red when >90% used
3. **Left-click** the tray icon to see the detailed popup with:
   - 5-hour window usage and reset time
   - Weekly window usage and reset time
   - Account information (if available)
4. **Right-click** for the context menu with:
   - Refresh Now
   - Refresh Interval settings
   - Settings
   - About
   - Exit

## Configuration

Access Settings via right-click menu or the popup window:

- **Refresh Interval**: How often to check for updated quota data (default: 5 minutes)

## How It Works

ClaudeBox uses the Claude CLI to fetch your quota usage:

1. Runs `claude /usage` to get 5-hour and weekly quota percentages
2. Runs `claude /status` to get account information
3. Parses the output (supports both JSON and text formats)
4. Displays the data in the system tray and popup

All data stays local - no network calls are made beyond what the Claude CLI does.

## Troubleshooting

### "Claude CLI not found"
- Ensure the Claude CLI is installed and in your PATH
- Try running `claude --version` in a terminal to verify installation

### No usage data shown
- Make sure you're logged in: `claude login`
- Run `claude /usage` manually to generate initial data

### Icon not visible
- Check if the icon is hidden in the system tray overflow area
- Right-click the taskbar → Taskbar settings → Turn on "Always show icons in notification area"

## Development

### Building

```bash
# Restore dependencies
dotnet restore ClaudeBox.sln

# Build
dotnet build ClaudeBox.sln

# Run (Windows only)
dotnet run --project src/ClaudeBox/ClaudeBox/ClaudeBox.csproj
```

### Testing

```bash
# Run tests (Windows only - requires Windows Desktop runtime)
dotnet test tests/ClaudeBox.Tests/ClaudeBox.Tests/ClaudeBox.Tests.csproj
```

### Project Structure

```
src/ClaudeBox/ClaudeBox/
├── App.xaml(.cs)           # Application entry point and tray icon setup
├── Models/
│   └── ClaudeUsage.cs      # Data models for Claude usage
├── Services/
│   ├── ClaudeCliService.cs # Service for interacting with Claude CLI
│   └── IconGeneratorService.cs # Generates dynamic tray icons
├── ViewModels/
│   └── MainViewModel.cs    # Main view model with quota data
├── Views/
│   ├── QuotaPopup.xaml(.cs)       # Popup control with progress bars
│   ├── QuotaPopupWindow.xaml(.cs) # Window hosting the popup
│   └── SettingsWindow.xaml(.cs)   # Settings dialog
└── Converters/
    └── Converters.cs       # WPF value converters

tests/ClaudeBox.Tests/ClaudeBox.Tests/
└── ClaudeUsageTests.cs     # Unit tests for models
```

### Technologies

- .NET 8.0 / WPF
- [H.NotifyIcon.Wpf](https://github.com/HavenDV/H.NotifyIcon) - System tray icon support
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) - MVVM framework
- System.Drawing - Dynamic icon generation

## Related Projects

- [CodexBar](https://github.com/steipete/CodexBar) - macOS menu bar app for Codex and Claude Code usage

## License

MIT License - see [LICENSE](LICENSE) for details.

## Author

[Brendan Kowitz](https://github.com/brendankowitz)
