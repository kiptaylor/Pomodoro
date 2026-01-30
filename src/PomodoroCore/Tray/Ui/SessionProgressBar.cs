using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace PomodoroTray.Ui;

/// <summary>
/// Owner-drawn progress indicator so we can apply state colors (Focus/Break/Paused)
/// without fighting the built-in WinForms <see cref="ProgressBar"/>.
/// </summary>
internal sealed class SessionProgressBar : Control
{
    private double progress;

    /// <summary>
    /// Progress in the range [0..1]. In this app we use it as "remaining fraction".
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double Progress
    {
        get => progress;
        set
        {
            var clamped = Math.Max(0, Math.Min(1, value));
            if (Math.Abs(progress - clamped) < 0.0001) return;
            progress = clamped;
            Invalidate();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color TrackColor { get; set; } = ThemeTokens.ProgressTrackColor;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color FillColor { get; set; } = ThemeTokens.FocusStateColor;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor { get; set; } = ThemeTokens.ProgressBorderColor;

    public SessionProgressBar()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint |
            ControlStyles.ResizeRedraw,
            true);

        TabStop = false;
        AccessibleRole = AccessibleRole.ProgressBar;

        Height = ThemeTokens.ProgressRowHeight;
        MinimumSize = new Size(0, ThemeTokens.ProgressRowHeight);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var rect = ClientRectangle;
        if (rect.Width <= 0 || rect.Height <= 0) return;

        // Keep a 1px border.
        var inner = Rectangle.Inflate(rect, -1, -1);
        if (inner.Width <= 0 || inner.Height <= 0) return;

        using (var track = new SolidBrush(TrackColor))
        {
            e.Graphics.FillRectangle(track, inner);
        }

        var fillWidth = (int)Math.Round(inner.Width * Progress);
        if (fillWidth > 0)
        {
            var fillRect = new Rectangle(inner.X, inner.Y, fillWidth, inner.Height);
            using var fill = new SolidBrush(FillColor);
            e.Graphics.FillRectangle(fill, fillRect);
        }

        using var border = new Pen(BorderColor);
        e.Graphics.DrawRectangle(border, inner);
    }
}
