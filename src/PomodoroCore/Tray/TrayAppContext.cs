using System.IO.Pipes;
using System.Media;
using System.Text;
using System.Text.Json;
using PomodoroCore;

namespace PomodoroTray;

internal sealed class TrayAppContext : ApplicationContext
{
    private readonly Store store;
    private readonly TaskIntentState intentState;
    private readonly NotifyIcon trayIcon;
    private readonly System.Windows.Forms.Timer timer;
    private readonly SynchronizationContext ui;
    private readonly CancellationTokenSource cts = new();
    private readonly Task ipcTask;
    private readonly MainForm mainForm;
    private bool lastBalloonWasTrayHint;

    internal event Action? IntentChanged;

    // Tray context menu items (dynamic "remote control")
    private readonly ToolStripMenuItem headerMenuItem = new() { Enabled = false };
    private readonly ToolStripMenuItem primaryActionMenuItem = new();
    private readonly ToolStripMenuItem skipMenuItem = new();
    private readonly ToolStripMenuItem resetMenuItem = new();

    private readonly ToolStripMenuItem setTaskMenuItem = new() { Text = "Set task" };
    private readonly ToolStripMenuItem openMenuItem = new();
    private readonly ToolStripMenuItem settingsMenuItem = new();
    private readonly ToolStripMenuItem quitMenuItem = new();

    public TrayAppContext(bool startHidden)
    {
        ui = SynchronizationContext.Current ?? new SynchronizationContext();
        store = new Store();
        intentState = store.LoadTaskIntentState();

        mainForm = new MainForm(this, store)
        {
            ShowInTaskbar = true
        };
        MainForm = mainForm;

        trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Visible = true,
            Text = "Pomodoro"
        };

        trayIcon.MouseUp += (_, e) =>
        {
            if (e.Button == MouseButtons.Left) ShowMainWindow();
        };
        trayIcon.BalloonTipClicked += (_, _) =>
        {
            if (lastBalloonWasTrayHint) ShowMainWindow();
        };
        trayIcon.ContextMenuStrip = BuildMenu();

        timer = new System.Windows.Forms.Timer { Interval = 1000 };
        timer.Tick += (_, _) => Tick();
        timer.Start();

        ipcTask = Task.Run(() => RunIpcServerAsync(cts.Token));

        if (!startHidden)
        {
            mainForm.Show();
            mainForm.Activate();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            timer.Stop();
            timer.Dispose();

            trayIcon.Visible = false;
            trayIcon.Dispose();

            cts.Cancel();
            cts.Dispose();
        }

        base.Dispose(disposing);
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();

        menu.Items.Add(headerMenuItem);
        menu.Items.Add(new ToolStripSeparator());

        // Remote-control actions
        primaryActionMenuItem.Click += (_, _) => PerformPrimaryAction();
        skipMenuItem.Text = "Skip";
        skipMenuItem.Click += (_, _) => Skip();
        resetMenuItem.Text = "Reset";
        resetMenuItem.Click += (_, _) => Stop();

        // Window actions
        openMenuItem.Text = "Open/Show Window";
        openMenuItem.Click += (_, _) => ShowMainWindow();
        settingsMenuItem.Text = "Settings";
        settingsMenuItem.Click += (_, _) => ShowSettings();

        // App actions
        quitMenuItem.Text = "Quit";
        quitMenuItem.Click += (_, _) => Exit();

        menu.Items.Add(primaryActionMenuItem);
        menu.Items.Add(skipMenuItem);
        menu.Items.Add(resetMenuItem);

        menu.Items.Add(setTaskMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(openMenuItem);
        menu.Items.Add(settingsMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(quitMenuItem);

        // Ensure state is correct at the moment the user opens the menu.
        menu.Opening += (_, _) => RefreshTrayMenu();

        RefreshTrayMenu();

        return menu;
    }

    private void RefreshTrayMenu(PomodoroState? state = null)
    {
        state ??= store.TryLoadState();

        // Header: state + remaining + intent (truncate for safety).
        headerMenuItem.Text = BuildTrayHeaderText(state, DateTimeOffset.UtcNow);

        BuildSetTaskSubMenu();

        if (state is null)
        {
            primaryActionMenuItem.Text = "Start";
            primaryActionMenuItem.Enabled = true;

            skipMenuItem.Enabled = false;
            resetMenuItem.Enabled = false;
            return;
        }

        primaryActionMenuItem.Text = state.IsPaused ? "Resume" : "Pause";
        primaryActionMenuItem.Enabled = true;

        // Skip/Reset are meaningful whenever a session exists.
        skipMenuItem.Enabled = true;
        resetMenuItem.Enabled = true;
    }

    private void PerformPrimaryAction()
    {
        var state = store.TryLoadState();
        if (state is null)
        {
            Start(force: false);
            return;
        }

        if (state.IsPaused)
        {
            Resume();
            return;
        }

        Pause();
    }

    private void ShowSettings()
    {
        ShowMainWindow();
        mainForm.FocusSettings();
    }

    internal void Exit()
    {
        ExitThread();
    }

    internal void HideToTray()
    {
        mainForm.Hide();
        ShowTrayHintBalloon("Pomodoro", "Still running in tray. Click to open.");
    }

    internal void ShowMainWindow()
    {
        if (mainForm.Visible)
        {
            mainForm.WindowState = FormWindowState.Normal;
            mainForm.Activate();
            return;
        }

        mainForm.Show();
        mainForm.WindowState = FormWindowState.Normal;
        mainForm.Activate();
    }

    private void Tick()
    {
        var state = store.TryLoadState();
        if (state is null)
        {
            trayIcon.Text = TruncateTooltip(BuildTrayHeaderText(null, DateTimeOffset.UtcNow));
            RefreshTrayMenu(null);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var remaining = state.GetRemainingSeconds(now);
        trayIcon.Text = TruncateTooltip(BuildTrayHeaderText(state, now));

        RefreshTrayMenu(state);

        if (state.IsPaused) return;
        if (remaining > 0) return;

        HandlePhaseEnd(state, nowUtc: now, forceAdvance: false);
    }

    private void HandlePhaseEnd(PomodoroState state, DateTimeOffset nowUtc, bool forceAdvance)
    {
        store.AppendLog(new LogEvent("phase_ended", nowUtc, state));

        if (state.Sound) SystemSounds.Exclamation.Play();
        if (state.Popup) ShowBalloon("Pomodoro", $"{state.Phase} complete.");

        if (!forceAdvance && !state.AutoAdvance)
        {
            var paused = state.Pause(nowUtc);
            store.SaveState(paused);
            RefreshTrayMenu(paused);
            return;
        }

        var result = state.AdvanceTo(nowUtc);
        if (result.Completed)
        {
            store.AppendLog(new LogEvent("session_completed", nowUtc, state));
            store.DeleteState();
            ShowBalloon("Pomodoro", "Session complete.");
            RefreshTrayMenu(null);
            return;
        }

        var next = result.State!;
        store.SaveState(next);
        store.AppendLog(new LogEvent("phase_started", nowUtc, next));
        ShowBalloon("Pomodoro", $"Now: {next.Phase} ({next.CycleIndex}/{next.Cycles})");
        RefreshTrayMenu(next);
    }

    internal void Skip()
    {
        var state = store.TryLoadState();
        if (state is null)
        {
            ShowBalloon("Pomodoro", "No active session.");
            RefreshTrayMenu(null);
            return;
        }

        var now = DateTimeOffset.UtcNow;

        // Force the current phase to be considered "ended" at now, even if paused.
        // We then route through the same phase-end logic (with forceAdvance) to keep behavior consistent.
        var forcedEnd = state with
        {
            IsPaused = false,
            PausedAtUtc = null,
            PausedRemainingSeconds = null,
            PhaseStartedAtUtc = now,
            PhaseDurationSeconds = 0
        };

        HandlePhaseEnd(forcedEnd, nowUtc: now, forceAdvance: true);
    }

    internal void Start(bool force, Dictionary<string, string?>? overrides = null)
    {
        var existing = store.TryLoadState();
        if (existing is not null && !force)
        {
            ShowBalloon("Pomodoro", "Session already running. Use Start (Force) to replace.");
            return;
        }

        var config = store.LoadOrCreateConfig();
        var options = OptionsFromConfig(config, overrides);
        var now = DateTimeOffset.UtcNow;
        var state = PomodoroState.New(options, now);

        store.SaveState(state);
        store.AppendLog(new LogEvent("session_started", now, state));

        var workMinutes = options.WorkSeconds / 60;
        ShowBalloon("Pomodoro", $"Started: {state.Phase} ({workMinutes} min)");

        RefreshTrayMenu(state);
    }

    internal void Pause()
    {
        var state = store.TryLoadState();
        if (state is null) { ShowBalloon("Pomodoro", "No active session."); return; }
        if (state.IsPaused) { ShowBalloon("Pomodoro", "Already paused."); return; }

        var now = DateTimeOffset.UtcNow;
        var paused = state.Pause(now);
        store.SaveState(paused);
        store.AppendLog(new LogEvent("paused", now, paused));
        ShowBalloon("Pomodoro", "Paused.");

        RefreshTrayMenu(paused);
    }

    internal void Resume()
    {
        var state = store.TryLoadState();
        if (state is null) { ShowBalloon("Pomodoro", "No active session."); return; }
        if (!state.IsPaused) { ShowBalloon("Pomodoro", "Not paused."); return; }

        var now = DateTimeOffset.UtcNow;
        var resumed = state.Resume(now);
        store.SaveState(resumed);
        store.AppendLog(new LogEvent("resumed", now, resumed));
        ShowBalloon("Pomodoro", "Resumed.");

        RefreshTrayMenu(resumed);
    }

    internal void Stop()
    {
        var state = store.TryLoadState();
        if (state is null) { ShowBalloon("Pomodoro", "No active session."); return; }

        var now = DateTimeOffset.UtcNow;
        store.AppendLog(new LogEvent("stopped", now, state));
        store.DeleteState();
        ShowBalloon("Pomodoro", "Stopped.");

        RefreshTrayMenu(null);
    }

    private void ShowStatusBalloon()
    {
        var state = store.TryLoadState();
        if (state is null)
        {
            ShowBalloon("Pomodoro", "No active session.");
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var remaining = Math.Max(0, state.GetRemainingSeconds(now));
        var text = state.IsPaused
            ? $"{state.Phase} {state.CycleIndex}/{state.Cycles} (paused)"
            : $"{state.Phase} {state.CycleIndex}/{state.Cycles} - {FormatDuration(TimeSpan.FromSeconds(remaining))} left";

        ShowBalloon("Pomodoro", text);
    }

    private void ShowBalloon(string title, string message)
    {
        try
        {
            lastBalloonWasTrayHint = false;
            trayIcon.ShowBalloonTip(3000, title, message, ToolTipIcon.Info);
        }
        catch
        {
            // ignore (e.g., explorer restarting)
        }
    }

    private void ShowTrayHintBalloon(string title, string message)
    {
        try
        {
            lastBalloonWasTrayHint = true;
            trayIcon.ShowBalloonTip(3000, title, message, ToolTipIcon.Info);
        }
        catch
        {
            // ignore (e.g., explorer restarting)
        }
    }

    private static SessionOptions OptionsFromConfig(PomodoroConfig config, Dictionary<string, string?>? overrides)
    {
        int GetMinutes(string name, int fallback)
        {
            if (overrides is null) return fallback;
            if (!overrides.TryGetValue(name, out var raw) || raw is null) return fallback;
            return int.TryParse(raw, out var v) ? v : fallback;
        }

        bool HasFlag(string name) => overrides?.ContainsKey(name) == true;

        var work = GetMinutes("--work", config.WorkMinutes);
        var brk = GetMinutes("--break", config.BreakMinutes);
        var lng = GetMinutes("--long", config.LongBreakMinutes);
        var cycles = GetMinutes("--cycles", config.Cycles);

        var auto = HasFlag("--auto") ? true : HasFlag("--no-auto") ? false : config.AutoAdvance;
        var popup = HasFlag("--popup") ? true : HasFlag("--no-popup") ? false : config.Popup;
        var sound = HasFlag("--sound") ? true : HasFlag("--no-sound") ? false : config.Sound;

        return new SessionOptions(
            WorkSeconds: Math.Max(1, work) * 60,
            BreakSeconds: Math.Max(1, brk) * 60,
            LongBreakSeconds: Math.Max(1, lng) * 60,
            Cycles: Math.Max(1, cycles),
            AutoAdvance: auto,
            Popup: popup,
            Sound: sound);
    }

    private static string FormatDuration(TimeSpan duration)
    {
        var totalMinutes = (int)duration.TotalMinutes;
        var seconds = duration.Seconds;
        return $"{totalMinutes:00}:{seconds:00}";
    }

    private static string TruncateTooltip(string text)
    {
        const int max = 63;
        return text.Length <= max ? text : text[..(max - 1)];
    }

    private string BuildTrayHeaderText(PomodoroState? state, DateTimeOffset nowUtc)
    {
        var intent = intentState.CurrentIntent;
        var intentPart = intent is null ? string.Empty : $" • {TaskIntentState.TruncateForUi(intent, 32)}";

        if (state is null)
        {
            return $"No active session{intentPart}";
        }

        var remaining = Math.Max(0, state.GetRemainingSeconds(nowUtc));
        var pausedPart = state.IsPaused ? " (paused)" : string.Empty;
        return $"{state.Phase} {state.CycleIndex}/{state.Cycles} - {FormatDuration(TimeSpan.FromSeconds(remaining))}{pausedPart}{intentPart}";
    }

    private void BuildSetTaskSubMenu()
    {
        setTaskMenuItem.DropDownItems.Clear();

        var current = intentState.CurrentIntent;
        if (current is not null)
        {
            var clear = new ToolStripMenuItem("Clear task")
            {
                ToolTipText = "Clear the current intent"
            };
            clear.Click += (_, _) => SetCurrentIntent(null, addToRecents: false);
            setTaskMenuItem.DropDownItems.Add(clear);
            setTaskMenuItem.DropDownItems.Add(new ToolStripSeparator());
        }

        if (intentState.Pinned.Count > 0)
        {
            setTaskMenuItem.DropDownItems.Add(new ToolStripMenuItem("Pinned") { Enabled = false });
            foreach (var item in intentState.Pinned)
            {
                var mi = new ToolStripMenuItem(TaskIntentState.TruncateForUi(item, 48))
                {
                    Checked = string.Equals(current, item, StringComparison.OrdinalIgnoreCase),
                    ToolTipText = item
                };
                mi.Click += (_, _) => SetCurrentIntent(item, addToRecents: true);
                setTaskMenuItem.DropDownItems.Add(mi);
            }
        }

        if (intentState.Recents.Count > 0)
        {
            if (setTaskMenuItem.DropDownItems.Count > 0) setTaskMenuItem.DropDownItems.Add(new ToolStripSeparator());
            setTaskMenuItem.DropDownItems.Add(new ToolStripMenuItem("Recent") { Enabled = false });
            foreach (var item in intentState.Recents)
            {
                var mi = new ToolStripMenuItem(TaskIntentState.TruncateForUi(item, 48))
                {
                    Checked = string.Equals(current, item, StringComparison.OrdinalIgnoreCase),
                    ToolTipText = item
                };
                mi.Click += (_, _) => SetCurrentIntent(item, addToRecents: true);
                setTaskMenuItem.DropDownItems.Add(mi);
            }
        }

        // If nothing to show, keep submenu disabled to reduce noise.
        setTaskMenuItem.Enabled = intentState.Pinned.Count > 0 || intentState.Recents.Count > 0 || intentState.CurrentIntent is not null;
    }

    internal string? GetCurrentIntent() => intentState.CurrentIntent;

    internal IReadOnlyList<string> GetPinnedIntents() => intentState.Pinned;

    internal IReadOnlyList<string> GetRecentIntents() => intentState.Recents;

    internal void SetCurrentIntent(string? raw, bool addToRecents)
    {
        var changed = intentState.SetCurrentIntent(raw, addToRecents);
        store.SaveTaskIntentState(intentState);
        if (changed) IntentChanged?.Invoke();
        RefreshTrayMenu();
    }

    internal void PinCurrentIntent()
    {
        if (intentState.Pin(intentState.CurrentIntent))
        {
            store.SaveTaskIntentState(intentState);
            IntentChanged?.Invoke();
            RefreshTrayMenu();
        }
    }

    internal void UnpinIntent(string? raw)
    {
        if (intentState.Unpin(raw))
        {
            store.SaveTaskIntentState(intentState);
            IntentChanged?.Invoke();
            RefreshTrayMenu();
        }
    }

    private async Task RunIpcServerAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await using var server = new NamedPipeServerStream(
                    PomodoroIpc.PipeName,
                    PipeDirection.InOut,
                    maxNumberOfServerInstances: 1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await server.WaitForConnectionAsync(token);

                using var reader = new StreamReader(server, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);
                await using var writer = new StreamWriter(server, Encoding.UTF8, bufferSize: 4096, leaveOpen: true) { AutoFlush = true };

                var line = await reader.ReadLineAsync(token);
                var response = HandleIpc(line);
                await writer.WriteLineAsync(JsonSerializer.Serialize(response));
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch
            {
                await Task.Delay(250, token);
            }
        }
    }

    private IpcResponse HandleIpc(string? line)
    {
        if (string.IsNullOrWhiteSpace(line)) return new IpcResponse(false, "Empty request.");

        IpcRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<IpcRequest>(line);
        }
        catch
        {
            return new IpcResponse(false, "Invalid JSON request.");
        }

        if (request is null || string.IsNullOrWhiteSpace(request.Command))
            return new IpcResponse(false, "Missing command.");

        var tcs = new TaskCompletionSource<IpcResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        ui.Post(_ =>
        {
            try
            {
                tcs.SetResult(ExecuteIpcOnUiThread(request));
            }
            catch (Exception ex)
            {
                tcs.SetResult(new IpcResponse(false, ex.Message));
            }
        }, null);

        return tcs.Task.Wait(TimeSpan.FromSeconds(2))
            ? tcs.Task.Result
            : new IpcResponse(false, "Tray command timed out.");
    }

    private IpcResponse ExecuteIpcOnUiThread(IpcRequest request)
    {
        var cmd = request.Command.ToLowerInvariant();

        switch (cmd)
        {
            case "ping":
                return new IpcResponse(true, "pong");
            case "open":
                ShowMainWindow();
                return new IpcResponse(true, "Opened.");
            case "start":
                Start(force: request.Options?.ContainsKey("--force") == true, overrides: request.Options);
                return new IpcResponse(true, "Started.");
            case "pause":
                Pause();
                return new IpcResponse(true, "Paused.");
            case "resume":
                Resume();
                return new IpcResponse(true, "Resumed.");
            case "stop":
                Stop();
                return new IpcResponse(true, "Stopped.");
            case "status":
                ShowStatusBalloon();
                return new IpcResponse(true, "Status shown.");
            case "exit":
                Exit();
                return new IpcResponse(true, "Exiting.");
            default:
                return new IpcResponse(false, $"Unknown IPC command: {request.Command}");
        }
    }
}

