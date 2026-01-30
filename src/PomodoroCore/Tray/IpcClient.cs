using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using PomodoroCore;

namespace PomodoroTray;

internal static class IpcClient
{
    public static bool TrySend(IpcRequest request, out IpcResponse response)
    {
        try
        {
            using var client = new NamedPipeClientStream(
                serverName: ".",
                pipeName: PomodoroIpc.PipeName,
                direction: PipeDirection.InOut);

            client.Connect(timeout: 150);

            using var writer = new StreamWriter(client, Encoding.UTF8, bufferSize: 4096, leaveOpen: true)
            {
                AutoFlush = true
            };
            using var reader = new StreamReader(client, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);

            writer.WriteLine(JsonSerializer.Serialize(request));
            var line = reader.ReadLine();

            if (string.IsNullOrWhiteSpace(line))
            {
                response = new IpcResponse(false, "No response.");
                return false;
            }

            response = JsonSerializer.Deserialize<IpcResponse>(line) ?? new IpcResponse(false, "Invalid response.");
            return response.Ok;
        }
        catch
        {
            response = new IpcResponse(false, "Not running.");
            return false;
        }
    }
}

