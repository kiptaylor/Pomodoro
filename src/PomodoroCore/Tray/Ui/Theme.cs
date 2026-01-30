using System.Drawing;
using System.Windows.Forms;

namespace PomodoroTray.Ui;

internal static class Theme
{
    public static void Apply(Control root)
    {
        if (root is null) return;
        ApplyRecursive(root, root);
    }

    private static void ApplyRecursive(Control root, Control node)
    {
        ApplySingle(root, node);

        foreach (Control child in node.Controls)
        {
            ApplyRecursive(root, child);
        }
    }

    private static void ApplySingle(Control root, Control control)
    {
        // Form/root level defaults.
        if (control is Form form)
        {
            // Preserve the classic look by sticking to system colors/fonts.
            if (form.BackColor.IsEmpty) form.BackColor = ThemeTokens.WindowBackColor;
            if (form.ForeColor.IsEmpty) form.ForeColor = ThemeTokens.WindowForeColor;

            // Avoid overriding custom fonts, but ensure a sane system default when the default font is used.
            if (ReferenceEquals(form.Font, Control.DefaultFont))
            {
                form.Font = ThemeTokens.BaseFont;
            }
        }

        switch (control)
        {
            case Label label:
                ApplyLabel(label);
                break;
            case Button button:
                ApplyButton(root, button);
                break;
            case CheckBox checkBox:
                ApplyCheckBox(checkBox);
                break;
            case GroupBox groupBox:
                ApplyGroupBox(root, groupBox);
                break;
            case FlowLayoutPanel flow:
                ApplyFlowLayoutPanel(root, flow);
                break;
        }
    }

    private static void ApplyLabel(Label label)
    {
        // Helps avoid truncated labels when we use fixed heights.
        if (!label.AutoSize)
        {
            label.AutoEllipsis = true;
        }

        // Avoid mnemonic underscores in normal UI labels.
        label.UseMnemonic = false;
    }

    private static void ApplyButton(Control root, Button button)
    {
        // Preserve standard WinForms visuals: do NOT force FlatStyle.

        // "Safe" sizing defaults.
        if (!button.AutoSize)
        {
            button.AutoSize = true;
            button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        }

        // Only apply minimum size if it wasn't explicitly set.
        if (button.MinimumSize.Width <= 0)
        {
            button.MinimumSize = new Size(ScalePx(root, ThemeTokens.ButtonMinWidth), 0);
        }

        // Standard spacing when the caller didn't specify a custom margin.
        if (IsDefaultMargin(button.Margin))
        {
            button.Margin = new Padding(0, 0, ScalePx(root, ThemeTokens.SpacingSm), 0);
        }
    }

    private static void ApplyCheckBox(CheckBox checkBox)
    {
        // AutoSize is generally safe for checkboxes and improves DPI behavior.
        if (!checkBox.AutoSize)
        {
            checkBox.AutoSize = true;
        }
    }

    private static void ApplyGroupBox(Control root, GroupBox groupBox)
    {
        // Give the contents some breathing room. Only apply if it looks default-ish.
        if (groupBox.Padding.All <= 3)
        {
            groupBox.Padding = new Padding(ScalePx(root, ThemeTokens.SpacingMd));
        }
    }

    private static void ApplyFlowLayoutPanel(Control root, FlowLayoutPanel flow)
    {
        // Common for button bars.
        if (flow.Padding == Padding.Empty)
        {
            flow.Padding = new Padding(ScalePx(root, ThemeTokens.SpacingMd));
        }

        if (flow.Margin == Padding.Empty)
        {
            flow.Margin = new Padding(0);
        }
    }

    private static bool IsDefaultMargin(Padding margin)
        // WinForms default margin is typically 3,3,3,3.
        => margin.Left == 3 && margin.Top == 3 && margin.Right == 3 && margin.Bottom == 3;

    /// <summary>
    /// DPI-safe scaling that avoids double-scaling:
    /// - If the handle isn't created yet, return the 96-DPI logical value (WinForms will autoscale later).
    /// - If the handle exists, scale immediately using the current DeviceDpi.
    /// </summary>
    private static int ScalePx(Control root, int logical96)
    {
        if (!root.IsHandleCreated) return logical96;
        return (int)Math.Round(logical96 * (root.DeviceDpi / (double)ThemeTokens.DesignDpi));
    }
}

