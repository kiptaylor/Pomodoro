using PomodoroCore;
using PomodoroTray.Ui;

namespace PomodoroTray;

internal sealed class MainForm : Form
{
    private readonly TrayAppContext app;
    private readonly Store store;
    private readonly System.Windows.Forms.Timer uiTimer;

    private readonly Label titleLabel;
    private readonly Label statusLabel;
    private readonly Label detailLabel;
    private readonly Label timeLabel;
    private readonly Label intentDisplayLabel;
    private readonly SessionProgressBar progressBar;
    private readonly Panel progressHost;
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

    private readonly TextBox intentInput;
    private readonly Button setIntentButton;
    private readonly Button pinIntentButton;
    private readonly Button clearIntentButton;
    private readonly ComboBox recentIntentCombo;
    private readonly ListBox pinnedListBox;
    private readonly Button unpinButton;

    private bool suppressIntentUi;

    private bool allowClose;

    internal void FocusSettings()
    {
        // Basic affordance for tray "Settings" menu.
        // Focus the first settings input so keyboard users can start adjusting immediately.
        workMinutesInput.Focus();
    }

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

        // Runtime-built forms don't get the designer's AutoScaleDimensions assignment.
        // Without a 96-DPI baseline, Dpi autoscaling becomes a no-op at 125%+ and text can clip badly.
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(ThemeTokens.DesignDpi, ThemeTokens.DesignDpi);

        Font = ThemeTokens.BaseFont;
        ClientSize = ThemeTokens.MainWindowClientSize;
        MinimumSize = ThemeTokens.MainWindowMinimumSize;

        titleLabel = new Label
        {
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font(Font.FontFamily, 14, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = ThemeTokens.TitleRowHeight,
            Text = "Pomodoro"
        };

        statusLabel = new Label
        {
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font(Font.FontFamily, 12, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = ThemeTokens.StatusRowHeight + 6,
            Text = "No active session"
        };

        detailLabel = new Label
        {
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = ThemeTokens.StatusRowHeight,
            ForeColor = ThemeTokens.SecondaryTextColor,
            Text = ""
        };

        intentDisplayLabel = new Label
        {
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = ThemeTokens.StatusRowHeight,
            ForeColor = ThemeTokens.SecondaryTextColor,
            Text = ""
        };

        timeLabel = new Label
        {
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font(Font.FontFamily, 32, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = ThemeTokens.TimerRowHeight + 10,
            Text = "--:--"
        };

        progressBar = new SessionProgressBar
        {
            Dock = DockStyle.Fill,
            TrackColor = ThemeTokens.ProgressTrackColor,
            FillColor = ThemeTokens.FocusStateColor,
            BorderColor = ThemeTokens.ProgressBorderColor,
            Progress = 0
        };

        progressHost = new Panel
        {
            Dock = DockStyle.Top,
            Height = ThemeTokens.ProgressRowHeight + ThemeTokens.SpacingSm * 2,
            Padding = new Padding(ThemeTokens.SpacingMd, ThemeTokens.SpacingSm, ThemeTokens.SpacingMd, ThemeTokens.SpacingSm),
            Margin = new Padding(0)
        };
        progressHost.Controls.Add(progressBar);

        intentInput = new TextBox { PlaceholderText = "e.g., Write report" };
        setIntentButton = new Button { Text = "Set" };
        pinIntentButton = new Button { Text = "Pin" };
        clearIntentButton = new Button { Text = "Clear" };

        recentIntentCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 200
        };

        pinnedListBox = new ListBox
        {
            IntegralHeight = false,
            Height = 90
        };

        unpinButton = new Button { Text = "Remove" };

        startButton = new Button { Text = "Start" };
        pauseButton = new Button { Text = "Pause" };
        resumeButton = new Button { Text = "Resume" };
        stopButton = new Button { Text = "Stop" };
        saveConfigButton = new Button { Text = "Save" };
        forceStartCheckBox = new CheckBox { Text = "Force" };

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

        setIntentButton.Click += (_, _) => CommitIntentFromInput(addToRecents: true);
        clearIntentButton.Click += (_, _) => app.SetCurrentIntent(null, addToRecents: false);
        pinIntentButton.Click += (_, _) => { CommitIntentFromInput(addToRecents: true); app.PinCurrentIntent(); };
        unpinButton.Click += (_, _) =>
        {
            if (pinnedListBox.SelectedItem is string item) app.UnpinIntent(item);
        };

        intentInput.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.Handled = true;
            e.SuppressKeyPress = true;
            CommitIntentFromInput(addToRecents: true);
        };

        pinnedListBox.Click += (_, _) =>
        {
            if (suppressIntentUi) return;
            if (pinnedListBox.SelectedItem is string item) app.SetCurrentIntent(item, addToRecents: true);
        };

        recentIntentCombo.SelectionChangeCommitted += (_, _) =>
        {
            if (suppressIntentUi) return;
            if (recentIntentCombo.SelectedItem is string item) app.SetCurrentIntent(item, addToRecents: true);
        };

        var settingsBox = BuildSettingsBox();
        var intentBox = BuildIntentBox();
        var buttons = BuildActionButtonsBar();

        // Docking order matters; keep the bottom action row visible and let settings take the remaining space.
        Controls.Add(settingsBox);
        Controls.Add(intentBox);
        Controls.Add(progressHost);
        Controls.Add(intentDisplayLabel);
        Controls.Add(timeLabel);
        Controls.Add(detailLabel);
        Controls.Add(statusLabel);
        Controls.Add(titleLabel);
        Controls.Add(buttons);

        // Apply centralized styling recursively ("CSS for WinForms").
        this.ApplyTheme();

        LoadConfigIntoInputs();

        uiTimer = new System.Windows.Forms.Timer { Interval = 250 };
        uiTimer.Tick += (_, _) => RefreshUi();
        uiTimer.Start();

        app.IntentChanged += RefreshIntentUi;

        FormClosing += OnFormClosing;
        Shown += (_, _) => RefreshUi();
        Shown += (_, _) => RefreshIntentUi();
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
            detailLabel.Text = "";
            timeLabel.Text = "--:--";

            statusLabel.ForeColor = ThemeTokens.WindowForeColor;
            detailLabel.ForeColor = ThemeTokens.SecondaryTextColor;
            timeLabel.ForeColor = ThemeTokens.WindowForeColor;

            progressBar.Progress = 0;
            progressBar.FillColor = ThemeTokens.ProgressBorderColor;

            startButton.Enabled = true;
            pauseButton.Enabled = false;
            resumeButton.Enabled = false;
            stopButton.Enabled = false;

            intentDisplayLabel.Text = "";
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var remainingSeconds = Math.Max(0, state.GetRemainingSeconds(now));
        var remaining = TimeSpan.FromSeconds(remainingSeconds);

        var durationSeconds = Math.Max(1, state.PhaseDurationSeconds);
        var remainingFraction = Math.Max(0, Math.Min(1, remainingSeconds / (double)durationSeconds));

        var phaseText = state.Phase switch
        {
            PomodoroPhase.Work => "Focus",
            PomodoroPhase.Break => "Short Break",
            PomodoroPhase.LongBreak => "Long Break",
            _ => state.Phase.ToString()
        };

        var accent = state.IsPaused
            ? ThemeTokens.PausedStateColor
            : state.Phase == PomodoroPhase.Work
                ? ThemeTokens.FocusStateColor
                : ThemeTokens.BreakStateColor;

        statusLabel.Text = state.IsPaused ? "Paused" : phaseText;
        detailLabel.Text = state.IsPaused
            ? $"{phaseText} • Cycle {state.CycleIndex} of {state.Cycles}"
            : $"Cycle {state.CycleIndex} of {state.Cycles}";

        statusLabel.ForeColor = accent;
        detailLabel.ForeColor = ThemeTokens.SecondaryTextColor;
        timeLabel.ForeColor = accent;

        progressBar.Progress = remainingFraction;
        progressBar.FillColor = accent;
        timeLabel.Text = $"{(int)remaining.TotalMinutes:00}:{remaining.Seconds:00}";

        var intent = app.GetCurrentIntent();
        intentDisplayLabel.Text = intent is null ? string.Empty : $"Working on: {TaskIntentState.TruncateForUi(intent, 60)}";

        startButton.Enabled = true;
        pauseButton.Enabled = !state.IsPaused;
        resumeButton.Enabled = state.IsPaused;
        stopButton.Enabled = true;
    }

    private GroupBox BuildIntentBox()
    {
        var box = new GroupBox
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(10),
            Text = "Current Intent"
        };

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 3,
            RowCount = 3,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0)
        };

        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        grid.Controls.Add(MakeLabel("Working on…"), 0, 0);
        grid.Controls.Add(intentInput, 1, 0);

        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0)
        };
        actions.Controls.Add(setIntentButton);
        actions.Controls.Add(pinIntentButton);
        actions.Controls.Add(clearIntentButton);
        grid.Controls.Add(actions, 2, 0);

        grid.Controls.Add(MakeLabel("Recent"), 0, 1);
        grid.Controls.Add(recentIntentCombo, 1, 1);
        grid.SetColumnSpan(recentIntentCombo, 2);
        recentIntentCombo.Margin = new Padding(0, 6, 0, 0);

        grid.Controls.Add(MakeLabel("Pinned"), 0, 2);

        var pinnedRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 6, 0, 0)
        };
        pinnedRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        pinnedRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        pinnedRow.Controls.Add(pinnedListBox, 0, 0);
        pinnedRow.Controls.Add(unpinButton, 1, 0);
        unpinButton.Margin = new Padding(6, 0, 0, 0);
        pinnedListBox.Dock = DockStyle.Fill;
        pinnedRow.Dock = DockStyle.Fill;

        grid.Controls.Add(pinnedRow, 1, 2);
        grid.SetColumnSpan(pinnedRow, 2);

        box.Controls.Add(grid);
        return box;
    }

    private void CommitIntentFromInput(bool addToRecents)
    {
        app.SetCurrentIntent(intentInput.Text, addToRecents);
    }

    private void RefreshIntentUi()
    {
        suppressIntentUi = true;
        try
        {
            var current = app.GetCurrentIntent();

            // Only overwrite the input if the user isn't actively editing.
            if (!intentInput.Focused)
            {
                intentInput.Text = current ?? string.Empty;
            }

            pinnedListBox.BeginUpdate();
            pinnedListBox.Items.Clear();
            foreach (var item in app.GetPinnedIntents()) pinnedListBox.Items.Add(item);
            pinnedListBox.EndUpdate();

            recentIntentCombo.BeginUpdate();
            recentIntentCombo.Items.Clear();
            foreach (var item in app.GetRecentIntents()) recentIntentCombo.Items.Add(item);
            recentIntentCombo.EndUpdate();

            // Sync selection (best-effort, ignore if not present).
            if (current is not null)
            {
                var pinnedIndex = IndexOfIgnoreCase(pinnedListBox.Items, current);
                if (pinnedIndex >= 0) pinnedListBox.SelectedIndex = pinnedIndex;

                var recentIndex = IndexOfIgnoreCase(recentIntentCombo.Items, current);
                if (recentIndex >= 0) recentIntentCombo.SelectedIndex = recentIndex;
            }
        }
        finally
        {
            suppressIntentUi = false;
        }
    }

    private static int IndexOfIgnoreCase(ListBox.ObjectCollection items, string value)
    {
        for (var i = 0; i < items.Count; i++)
        {
            if (items[i] is string s && string.Equals(s, value, StringComparison.OrdinalIgnoreCase)) return i;
        }

        return -1;
    }

    private static int IndexOfIgnoreCase(ComboBox.ObjectCollection items, string value)
    {
        for (var i = 0; i < items.Count; i++)
        {
            if (items[i] is string s && string.Equals(s, value, StringComparison.OrdinalIgnoreCase)) return i;
        }

        return -1;
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
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            Text = "Settings (next start)"
        };

        // Make settings scroll instead of pushing the bottom action row off-screen.
        var scroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Margin = new Padding(0)
        };

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 4,
            RowCount = 4,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0)
        };

        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

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
        autoAdvanceCheckBox.Margin = new Padding(0, 6, 0, 0);

        var flagsRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 6, 0, 0)
        };
        flagsRow.Controls.Add(popupCheckBox);
        flagsRow.Controls.Add(soundCheckBox);

        grid.Controls.Add(flagsRow, 0, 3);
        grid.SetColumnSpan(flagsRow, 4);

        scroll.Controls.Add(grid);
        box.Controls.Add(scroll);
        return box;
    }

    private FlowLayoutPanel BuildActionButtonsBar()
    {
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = Padding.Empty,
            WrapContents = true,
            Margin = new Padding(0)
        };

        buttons.Controls.Add(startButton);
        buttons.Controls.Add(forceStartCheckBox);
        buttons.Controls.Add(saveConfigButton);
        buttons.Controls.Add(pauseButton);
        buttons.Controls.Add(resumeButton);
        buttons.Controls.Add(stopButton);
        return buttons;
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
