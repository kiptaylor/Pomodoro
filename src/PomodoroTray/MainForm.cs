using PomodoroCore;

namespace PomodoroTray;

internal sealed class MainForm : Form
{
    private readonly TrayAppContext app;
    private readonly Store store;
    private readonly System.Windows.Forms.Timer uiTimer;

    private readonly Label titleLabel;
    private readonly Label statusLabel;
    private readonly Label timeLabel;
    private readonly Button startButton;
    private readonly Button pauseButton;
    private readonly Button resumeButton;
    private readonly Button stopButton;
    private readonly Button saveConfigButton;
    private readonly CheckBox forceStartCheckBox;

    private readonly NumericUpDown workMinutesInput;
    private readonly NumericUpDown breakMinutesInput;
    private readonly NumericUpDown longBreakMinutesInput;
    private readonly NumericUpDown cyclesInput;
    private readonly CheckBox autoAdvanceCheckBox;
    private readonly CheckBox popupCheckBox;
    private readonly CheckBox soundCheckBox;

    private bool allowClose;

    public MainForm(TrayAppContext app, Store store)
    {
        this.app = app;
        this.store = store;

        Text = "Pomodoro";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = true;
        KeyPreview = true;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(440, 380);
        MinimumSize = new Size(440, 380);

        titleLabel = new Label
        {
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font(Font.FontFamily, 14, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 44,
            Text = "Pomodoro"
        };

        statusLabel = new Label
        {
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 28,
            Text = "No active session"
        };

        timeLabel = new Label
        {
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font(Font.FontFamily, 28, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 72,
            Text = "--:--"
        };

        startButton = new Button { Text = "Start", Width = 80 };
        pauseButton = new Button { Text = "Pause", Width = 80 };
        resumeButton = new Button { Text = "Resume", Width = 80 };
        stopButton = new Button { Text = "Stop", Width = 80 };
        saveConfigButton = new Button { Text = "Save", Width = 80 };
        forceStartCheckBox = new CheckBox { Text = "Force", AutoSize = true };

        workMinutesInput = NewMinutesInput(max: 180);
        breakMinutesInput = NewMinutesInput(max: 60);
        longBreakMinutesInput = NewMinutesInput(max: 120);
        cyclesInput = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 12,
            Value = 4,
            Width = 80
        };
        autoAdvanceCheckBox = new CheckBox { Text = "Auto-advance", AutoSize = true };
        popupCheckBox = new CheckBox { Text = "Popup", AutoSize = true };
        soundCheckBox = new CheckBox { Text = "Sound", AutoSize = true };

        startButton.Click += (_, _) => app.Start(force: forceStartCheckBox.Checked, overrides: BuildOverrides());
        pauseButton.Click += (_, _) => app.Pause();
        resumeButton.Click += (_, _) => app.Resume();
        stopButton.Click += (_, _) => app.Stop();
        saveConfigButton.Click += (_, _) => SaveConfigFromInputs();

        var settingsBox = BuildSettingsBox();

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 66,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(10, 12, 10, 10),
            WrapContents = false
        };
        buttons.Controls.Add(startButton);
        buttons.Controls.Add(forceStartCheckBox);
        buttons.Controls.Add(saveConfigButton);
        buttons.Controls.Add(pauseButton);
        buttons.Controls.Add(resumeButton);
        buttons.Controls.Add(stopButton);

        Controls.Add(buttons);
        Controls.Add(settingsBox);
        Controls.Add(timeLabel);
        Controls.Add(statusLabel);
        Controls.Add(titleLabel);

        LoadConfigIntoInputs();

        uiTimer = new System.Windows.Forms.Timer { Interval = 250 };
        uiTimer.Tick += (_, _) => RefreshUi();
        uiTimer.Start();

        FormClosing += OnFormClosing;
        Shown += (_, _) => RefreshUi();
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                PromptTrayOrExit();
            }
        };
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (allowClose) return;
        if (e.CloseReason != CloseReason.UserClosing) return;

        e.Cancel = true;
        PromptTrayOrExit();
    }

    private void PromptTrayOrExit()
    {
        var result = MessageBox.Show(
            "Keep running in the tray?\n\nYes: go to tray\nNo: exit\nCancel: stay open",
            "Pomodoro",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            app.HideToTray();
            return;
        }

        if (result == DialogResult.No)
        {
            allowClose = true;
            app.Exit();
        }
    }

    private void RefreshUi()
    {
        var state = store.TryLoadState();
        if (state is null)
        {
            statusLabel.Text = "No active session";
            timeLabel.Text = "--:--";

            startButton.Enabled = true;
            pauseButton.Enabled = false;
            resumeButton.Enabled = false;
            stopButton.Enabled = false;
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var remainingSeconds = Math.Max(0, state.GetRemainingSeconds(now));
        var remaining = TimeSpan.FromSeconds(remainingSeconds);

        statusLabel.Text = $"{state.Phase} ({state.CycleIndex}/{state.Cycles})" + (state.IsPaused ? " - Paused" : "");
        timeLabel.Text = $"{(int)remaining.TotalMinutes:00}:{remaining.Seconds:00}";

        startButton.Enabled = true;
        pauseButton.Enabled = !state.IsPaused;
        resumeButton.Enabled = state.IsPaused;
        stopButton.Enabled = true;
    }

    private static NumericUpDown NewMinutesInput(int max)
        => new()
        {
            Minimum = 1,
            Maximum = max,
            Value = 25,
            Width = 80
        };

    private GroupBox BuildSettingsBox()
    {
        var box = new GroupBox
        {
            Dock = DockStyle.Top,
            Height = 155,
            Padding = new Padding(10),
            Text = "Settings (next start)"
        };

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 4,
            AutoSize = false
        };

        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));

        grid.Controls.Add(MakeLabel("Work"), 0, 0);
        grid.Controls.Add(workMinutesInput, 1, 0);
        grid.Controls.Add(MakeLabel("Break"), 2, 0);
        grid.Controls.Add(breakMinutesInput, 3, 0);

        grid.Controls.Add(MakeLabel("Long"), 0, 1);
        grid.Controls.Add(longBreakMinutesInput, 1, 1);
        grid.Controls.Add(MakeLabel("Cycles"), 2, 1);
        grid.Controls.Add(cyclesInput, 3, 1);

        grid.Controls.Add(autoAdvanceCheckBox, 0, 2);
        grid.SetColumnSpan(autoAdvanceCheckBox, 4);

        var flagsRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0)
        };
        flagsRow.Controls.Add(popupCheckBox);
        flagsRow.Controls.Add(soundCheckBox);

        grid.Controls.Add(flagsRow, 0, 3);
        grid.SetColumnSpan(flagsRow, 4);

        box.Controls.Add(grid);
        return box;
    }

    private static Label MakeLabel(string text)
        => new()
        {
            Text = text,
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill
        };

    private void LoadConfigIntoInputs()
    {
        var config = store.LoadOrCreateConfig();
        workMinutesInput.Value = ClampToRange(config.WorkMinutes, (int)workMinutesInput.Minimum, (int)workMinutesInput.Maximum);
        breakMinutesInput.Value = ClampToRange(config.BreakMinutes, (int)breakMinutesInput.Minimum, (int)breakMinutesInput.Maximum);
        longBreakMinutesInput.Value = ClampToRange(config.LongBreakMinutes, (int)longBreakMinutesInput.Minimum, (int)longBreakMinutesInput.Maximum);
        cyclesInput.Value = ClampToRange(config.Cycles, (int)cyclesInput.Minimum, (int)cyclesInput.Maximum);

        autoAdvanceCheckBox.Checked = config.AutoAdvance;
        popupCheckBox.Checked = config.Popup;
        soundCheckBox.Checked = config.Sound;
    }

    private void SaveConfigFromInputs()
    {
        var updated = new PomodoroConfig(
            WorkMinutes: (int)workMinutesInput.Value,
            BreakMinutes: (int)breakMinutesInput.Value,
            LongBreakMinutes: (int)longBreakMinutesInput.Value,
            Cycles: (int)cyclesInput.Value,
            AutoAdvance: autoAdvanceCheckBox.Checked,
            Popup: popupCheckBox.Checked,
            Sound: soundCheckBox.Checked);

        store.SaveConfig(updated);
    }

    private Dictionary<string, string?> BuildOverrides()
    {
        var overrides = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["--work"] = ((int)workMinutesInput.Value).ToString(),
            ["--break"] = ((int)breakMinutesInput.Value).ToString(),
            ["--long"] = ((int)longBreakMinutesInput.Value).ToString(),
            ["--cycles"] = ((int)cyclesInput.Value).ToString()
        };

        AddBoolFlag(overrides, autoAdvanceCheckBox.Checked, "--auto", "--no-auto");
        AddBoolFlag(overrides, popupCheckBox.Checked, "--popup", "--no-popup");
        AddBoolFlag(overrides, soundCheckBox.Checked, "--sound", "--no-sound");

        if (forceStartCheckBox.Checked) overrides["--force"] = null;

        return overrides;
    }

    private static void AddBoolFlag(Dictionary<string, string?> overrides, bool enabled, string enabledFlag, string disabledFlag)
    {
        overrides.Remove(enabledFlag);
        overrides.Remove(disabledFlag);
        overrides[enabled ? enabledFlag : disabledFlag] = null;
    }

    private static int ClampToRange(int value, int min, int max)
        => Math.Min(max, Math.Max(min, value));
}
