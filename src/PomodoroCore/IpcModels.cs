namespace PomodoroCore;

public static class PomodoroIpc
{
    public const string PipeName = "pomodoro.pom";
}

public sealed record IpcRequest(
    string Command,
    Dictionary<string, string?>? Options = null,
    List<string>? Positionals = null);

public sealed record IpcResponse(
    bool Ok,
    string Message,
    string? Payload = null);

