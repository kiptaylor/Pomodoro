namespace PomodoroTray;

internal sealed class CliArgs
{
    private CliArgs(string? command, List<string> positionals, Dictionary<string, string?> options)
    {
        Command = command;
        Positionals = positionals;
        Options = options;
    }

    public string? Command { get; }
    public IReadOnlyList<string> Positionals { get; }
    public IReadOnlyDictionary<string, string?> Options { get; }

    public string? DataDirOverride
        => TryGet("--data-dir", out var v) ? v : null;

    public bool HasFlag(string name) => Options.ContainsKey(name);

    public bool TryGet(string name, out string? value) => Options.TryGetValue(name, out value);

    public bool TryGetInt(string name, out int value)
    {
        value = 0;
        if (!TryGet(name, out var raw) || raw is null) return false;
        return int.TryParse(raw, out value);
    }

    public static CliArgs Parse(string[] args)
    {
        if (args.Length == 0) return new CliArgs(null, new List<string>(), new Dictionary<string, string?>());

        var command = args[0];
        var positionals = new List<string>();
        var options = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        for (var i = 1; i < args.Length; i++)
        {
            var token = args[i];
            if (!token.StartsWith("--", StringComparison.Ordinal))
            {
                positionals.Add(token);
                continue;
            }

            if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                options[token] = args[i + 1];
                i++;
            }
            else
            {
                options[token] = null;
            }
        }

        return new CliArgs(command, positionals, options);
    }
}

