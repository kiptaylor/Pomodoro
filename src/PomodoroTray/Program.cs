using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using PomodoroCore;

namespace PomodoroTray;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var parsed = CliArgs.Parse(args);

        if (parsed.Command is null || parsed.Command is "help" or "--help" or "-h")
        {
            if (args.Length == 0)
            {
                // Double-click UX: no args => show UI (or activate existing instance).
            }
            else
            {
                Console.WriteLine(HelpText);
                return 0;
            }
        }

        if (parsed.Command is "where")
        {
            var store = new Store(parsed.DataDirOverride);
            Console.WriteLine($"DataDir: {store.DataDir}");
            Console.WriteLine($"Config:  {store.ConfigPath}");
            Console.WriteLine($"State:   {store.StatePath}");
            Console.WriteLine($"Log:     {store.LogPath}");
            return 0;
        }

        if (parsed.Command is "config")
        {
            return RunConfig(parsed);
        }

        using var mutex = new Mutex(initiallyOwned: true, name: "Pomodoro.SingleInstance", out var isFirstInstance);

        if (!isFirstInstance)
        {
            // Another instance is already running: forward commands or activate window.
            if (args.Length == 0)
            {
                IpcClient.TrySend(new IpcRequest("open"), out _);
                return 0;
            }

            if (TryForwardCommand(parsed, out var forwardedMessage))
            {
                if (!string.IsNullOrWhiteSpace(forwardedMessage)) Console.WriteLine(forwardedMessage);
                return 0;
            }

            Console.Error.WriteLine("Pomodoro is running, but failed to send command (is it still starting?).");
            return 1;
        }

        // First instance.
        if (args.Length == 0)
        {
            return RunApp(startHidden: false);
        }

        if (parsed.Command is "--background")
        {
            return RunApp(startHidden: true);
        }

        // CLI invoked, but no instance exists yet: start background instance, then forward command.
        StartBackgroundInstance();

        if (TryForwardCommand(parsed, out var msg))
        {
            if (!string.IsNullOrWhiteSpace(msg)) Console.WriteLine(msg);
            return 0;
        }

        Console.Error.WriteLine("Started Pomodoro, but couldn't deliver the command. Try again in a second.");
        return 1;
    }

    private static int RunApp(bool startHidden)
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new TrayAppContext(startHidden));
        return 0;
    }

    private static void StartBackgroundInstance()
    {
        var exe = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exe)) return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                Arguments = "--background",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
        }
        catch
        {
            // ignore
        }
    }

    private static bool TryForwardCommand(CliArgs parsed, out string message)
    {
        message = string.Empty;

        var cmd = parsed.Command?.ToLowerInvariant();
        if (cmd is null) return false;

        var request = cmd switch
        {
            "start" => new IpcRequest("start", new Dictionary<string, string?>(parsed.Options), new List<string>(parsed.Positionals)),
            "pause" => new IpcRequest("pause"),
            "resume" => new IpcRequest("resume"),
            "stop" => new IpcRequest("stop"),
            "status" => new IpcRequest("status"),
            "open" => new IpcRequest("open"),
            "exit" => new IpcRequest("exit"),
            _ => null
        };

        if (request is null) return false;

        // Give the background instance a moment to open its pipe.
        for (var attempt = 0; attempt < 20; attempt++)
        {
            if (IpcClient.TrySend(request, out var response))
            {
                message = response.Message;
                return true;
            }

            Thread.Sleep(100);
        }

        return false;
    }

    private static int RunConfig(CliArgs args)
    {
        var store = new Store(args.DataDirOverride);

        if (args.Positionals is ["set", var key, var value])
        {
            var config = store.LoadOrCreateConfig();
            var updated = UpdateConfig(config, key, value);
            store.SaveConfig(updated);
            Console.WriteLine("Updated config.");
            return 0;
        }

        var loaded = store.LoadOrCreateConfig();
        Console.WriteLine(JsonSerializer.Serialize(loaded, Store.JsonIndented));
        return 0;
    }

    private static PomodoroConfig UpdateConfig(PomodoroConfig config, string key, string value)
    {
        static int AsInt(string s) => int.TryParse(s, out var v) ? v : throw new Exception($"Invalid number: {s}");
        static bool AsBool(string s) =>
            s.Equals("true", StringComparison.OrdinalIgnoreCase) ||
            s.Equals("1", StringComparison.OrdinalIgnoreCase) ||
            s.Equals("yes", StringComparison.OrdinalIgnoreCase)
                ? true
                : s.Equals("false", StringComparison.OrdinalIgnoreCase) ||
                  s.Equals("0", StringComparison.OrdinalIgnoreCase) ||
                  s.Equals("no", StringComparison.OrdinalIgnoreCase)
                    ? false
                    : throw new Exception($"Invalid boolean: {s}");

        return key.ToLowerInvariant() switch
        {
            "work" => config with { WorkMinutes = AsInt(value) },
            "break" => config with { BreakMinutes = AsInt(value) },
            "long" => config with { LongBreakMinutes = AsInt(value) },
            "cycles" => config with { Cycles = AsInt(value) },
            "auto" => config with { AutoAdvance = AsBool(value) },
            "popup" => config with { Popup = AsBool(value) },
            "sound" => config with { Sound = AsBool(value) },
            _ => throw new Exception($"Unknown config key: {key}")
        };
    }

    private const string HelpText =
        """
        pom - all-in-one Pomodoro (Windows)

        Double-click:
          Opens the UI. Closing asks: tray or exit.

        From a terminal:
          pom start [options]     Start a session (starts tray app if needed)
          pom pause              Pause
          pom resume             Resume
          pom stop               Stop and clear
          pom open               Open the window
          pom exit               Exit the app
          pom status             Show status notification
          pom config             Show config JSON
          pom config set <k> <v> Set config (work/break/long/cycles/auto/popup/sound)
          pom where              Show data directory paths

        Options (start):
          --work <min>           Work minutes
          --break <min>          Break minutes
          --long <min>           Long-break minutes
          --cycles <n>           Work sessions per set
          --auto / --no-auto
          --popup / --no-popup
          --sound / --no-sound
          --force
        """;
}
