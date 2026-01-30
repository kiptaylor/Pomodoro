using System.Drawing;
using System.Windows.Forms;

namespace PomodoroTray.Ui;

/// <summary>
/// Single source of truth for the app's WinForms "theme tokens".
///
/// Tokens are intentionally conservative:
/// - Prefer system colors/fonts to preserve the classic WinForms look.
/// - Store sizes in 96-DPI logical units. The theme layer scales them safely.
/// </summary>
internal static class ThemeTokens
{
    /// <summary>
    /// The logical DPI baseline WinForms designers typically use.
    /// </summary>
    public const int DesignDpi = 96;

    // Colors: keep system defaults.
    public static Color WindowBackColor => SystemColors.Control;
    public static Color WindowForeColor => SystemColors.ControlText;

    // Fonts: keep system defaults.
    public static Font BaseFont => SystemFonts.MessageBoxFont ?? Control.DefaultFont;

    // Spacing / padding (96-DPI logical units).
    public const int SpacingXs = 6;
    public const int SpacingSm = 8;
    public const int SpacingMd = 10;

    // Common control sizing (96-DPI logical units).
    public const int ButtonMinWidth = 80;

    // Main window sizing (96-DPI logical units).
    public static Size MainWindowClientSize => new(440, 380);
    public static Size MainWindowMinimumSize => new(440, 380);

    // Header row heights (96-DPI logical units).
    public const int TitleRowHeight = 44;
    public const int StatusRowHeight = 28;
    public const int TimerRowHeight = 72;
}

