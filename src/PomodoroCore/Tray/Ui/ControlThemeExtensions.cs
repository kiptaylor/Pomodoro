using System.Windows.Forms;

namespace PomodoroTray.Ui;

internal static class ControlThemeExtensions
{
    public static void ApplyTheme(this Control root) => Theme.Apply(root);
}

