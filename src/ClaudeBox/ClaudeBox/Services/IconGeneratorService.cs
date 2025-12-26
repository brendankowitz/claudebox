using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Media.Imaging;

namespace ClaudeBox.Services;

/// <summary>
/// Service for generating dynamic tray icons showing quota progress.
/// </summary>
public class IconGeneratorService
{
    private const int IconSize = 16;
    private readonly Color _backgroundColor = Color.FromArgb(60, 60, 60);
    private readonly Color _progressColor = Color.FromArgb(255, 149, 98);  // Claude orange-ish color
    private readonly Color _warningColor = Color.FromArgb(255, 193, 7);    // Yellow warning
    private readonly Color _criticalColor = Color.FromArgb(244, 67, 54);   // Red critical
    private readonly Color _unknownColor = Color.FromArgb(128, 128, 128);  // Gray for unknown

    /// <summary>
    /// Generates a tray icon showing the weekly usage percentage.
    /// </summary>
    /// <param name="weeklyPercentUsed">Weekly usage percentage (0-100)</param>
    /// <param name="hourlyPercentUsed">Hourly usage percentage (0-100), optional for dual-bar display</param>
    /// <param name="isUnknown">If true, shows a gray icon indicating unknown status</param>
    /// <returns>Icon ready for use in system tray</returns>
    public Icon GenerateProgressIcon(double weeklyPercentUsed, double? hourlyPercentUsed = null, bool isUnknown = false)
    {
        using var bitmap = new Bitmap(IconSize, IconSize);
        using var graphics = Graphics.FromImage(bitmap);

        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        if (isUnknown)
        {
            DrawUnknownIcon(graphics);
        }
        else if (hourlyPercentUsed.HasValue)
        {
            DrawDualBarIcon(graphics, weeklyPercentUsed, hourlyPercentUsed.Value);
        }
        else
        {
            DrawCircularProgressIcon(graphics, weeklyPercentUsed);
        }

        return Icon.FromHandle(bitmap.GetHicon());
    }

    private void DrawCircularProgressIcon(Graphics graphics, double percentUsed)
    {
        // Draw background circle
        var rect = new Rectangle(1, 1, IconSize - 2, IconSize - 2);
        using var bgBrush = new SolidBrush(_backgroundColor);
        graphics.FillEllipse(bgBrush, rect);

        // Calculate remaining percentage (we show how much is LEFT, not used)
        var percentRemaining = 100 - percentUsed;
        
        // Draw progress arc showing remaining quota
        if (percentRemaining > 0)
        {
            var color = GetProgressColor(percentUsed);
            using var progressBrush = new SolidBrush(color);
            
            // Start from top (-90 degrees) and sweep based on remaining percentage
            var sweepAngle = (float)(percentRemaining * 3.6); // 360 degrees = 100%
            graphics.FillPie(progressBrush, rect, -90, sweepAngle);
        }

        // Draw inner circle for donut effect
        var innerRect = new Rectangle(4, 4, IconSize - 8, IconSize - 8);
        using var innerBrush = new SolidBrush(_backgroundColor);
        graphics.FillEllipse(innerBrush, innerRect);
    }

    private void DrawDualBarIcon(Graphics graphics, double weeklyPercent, double hourlyPercent)
    {
        // Draw two horizontal bars: top for hourly, bottom for weekly
        const int barHeight = 5;
        const int margin = 2;
        const int topBarY = margin;
        const int bottomBarY = IconSize - margin - barHeight;

        // Top bar (hourly) - background
        var topBarRect = new Rectangle(margin, topBarY, IconSize - 2 * margin, barHeight);
        using (var bgBrush = new SolidBrush(_backgroundColor))
        {
            graphics.FillRectangle(bgBrush, topBarRect);
        }

        // Top bar (hourly) - progress showing remaining
        var hourlyRemaining = 100 - hourlyPercent;
        if (hourlyRemaining > 0)
        {
            var progressWidth = (int)((IconSize - 2 * margin) * hourlyRemaining / 100);
            var progressRect = new Rectangle(margin, topBarY, progressWidth, barHeight);
            using var progressBrush = new SolidBrush(GetProgressColor(hourlyPercent));
            graphics.FillRectangle(progressBrush, progressRect);
        }

        // Bottom bar (weekly) - background
        var bottomBarRect = new Rectangle(margin, bottomBarY, IconSize - 2 * margin, barHeight);
        using (var bgBrush = new SolidBrush(_backgroundColor))
        {
            graphics.FillRectangle(bgBrush, bottomBarRect);
        }

        // Bottom bar (weekly) - progress showing remaining
        var weeklyRemaining = 100 - weeklyPercent;
        if (weeklyRemaining > 0)
        {
            var progressWidth = (int)((IconSize - 2 * margin) * weeklyRemaining / 100);
            var progressRect = new Rectangle(margin, bottomBarY, progressWidth, barHeight);
            using var progressBrush = new SolidBrush(GetProgressColor(weeklyPercent));
            graphics.FillRectangle(progressBrush, progressRect);
        }
    }

    private void DrawUnknownIcon(Graphics graphics)
    {
        // Draw a gray circle with a question mark appearance
        var rect = new Rectangle(1, 1, IconSize - 2, IconSize - 2);
        using var bgBrush = new SolidBrush(_unknownColor);
        graphics.FillEllipse(bgBrush, rect);

        // Draw a small dot in the center
        var dotRect = new Rectangle(6, 6, 4, 4);
        using var dotBrush = new SolidBrush(Color.White);
        graphics.FillEllipse(dotBrush, dotRect);
    }

    private Color GetProgressColor(double percentUsed)
    {
        if (percentUsed >= 90)
            return _criticalColor;
        if (percentUsed >= 75)
            return _warningColor;
        return _progressColor;
    }

    /// <summary>
    /// Converts a System.Drawing.Icon to a WPF BitmapSource.
    /// </summary>
    public static BitmapSource IconToBitmapSource(Icon icon)
    {
        using var bitmap = icon.ToBitmap();
        using var stream = new MemoryStream();
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        stream.Position = 0;

        var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        return decoder.Frames[0];
    }

    /// <summary>
    /// Generates a default icon for the application.
    /// </summary>
    public Icon GenerateDefaultIcon()
    {
        return GenerateProgressIcon(0, null, true);
    }

    /// <summary>
    /// Saves an icon to a file (useful for creating static resources).
    /// </summary>
    public void SaveIconToFile(Icon icon, string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Create);
        icon.Save(stream);
    }

    /// <summary>
    /// Creates and saves the default application icons.
    /// </summary>
    public void CreateDefaultIcons(string resourcesPath)
    {
        // Create main icon (full progress)
        using var mainIcon = GenerateProgressIcon(0);
        SaveIconToFile(mainIcon, Path.Combine(resourcesPath, "claudebox.ico"));

        // Create gray icon (unknown status)
        using var grayIcon = GenerateProgressIcon(0, null, true);
        SaveIconToFile(grayIcon, Path.Combine(resourcesPath, "claudebox-gray.ico"));
    }
}
