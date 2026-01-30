using System.Text.Json;

namespace PomodoroCore;

public sealed class Store
{
    public Store(string? dataDirOverride = null)
    {
        DataDir = dataDirOverride ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Pomodoro");

        Directory.CreateDirectory(DataDir);
    }

    public string DataDir { get; }
    public string ConfigPath => Path.Combine(DataDir, "config.json");
    public string StatePath => Path.Combine(DataDir, "state.json");
    public string LogPath => Path.Combine(DataDir, "log.jsonl");

    public static readonly JsonSerializerOptions JsonIndented = new()
    {
        WriteIndented = true
    };

    private static readonly JsonSerializerOptions JsonCompact = new()
    {
        WriteIndented = false
    };

    public PomodoroConfig LoadOrCreateConfig()
    {
        if (!File.Exists(ConfigPath))
        {
            var created = new PomodoroConfig();
            SaveConfig(created);
            return created;
        }

        return ReadJson<PomodoroConfig>(ConfigPath) ?? new PomodoroConfig();
    }

    public void SaveConfig(PomodoroConfig config) => WriteJson(ConfigPath, config, JsonIndented);

    public PomodoroState? TryLoadState()
    {
        if (!File.Exists(StatePath)) return null;
        return ReadJson<PomodoroState>(StatePath);
    }

    public void SaveState(PomodoroState state) => WriteJson(StatePath, state, JsonIndented);

    public void DeleteState()
    {
        if (File.Exists(StatePath)) File.Delete(StatePath);
    }

    public void AppendLog(LogEvent evt)
    {
        var line = JsonSerializer.Serialize(evt, JsonCompact);
        File.AppendAllText(LogPath, line + Environment.NewLine);
    }

    private static T? ReadJson<T>(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return default;
        }
    }

    private static void WriteJson<T>(string path, T value, JsonSerializerOptions options)
    {
        var temp = path + ".tmp";
        var json = JsonSerializer.Serialize(value, options);
        File.WriteAllText(temp, json);
        File.Move(temp, path, overwrite: true);
    }
}

