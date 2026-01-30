# Development

## Solution layout

- `src/PomodoroTray/` — WinForms app (UI + tray + IPC server)
- `src/PomodoroCore/` — shared core models + persistence

## Build

```powershell
$env:DOTNET_CLI_HOME = (Resolve-Path .dotnet_cli_home).Path
dotnet build src/PomodoroTray/PomodoroTray.csproj -c Release
```

## Notes

- The repository ignores build outputs and `pom.exe` so you don’t accidentally commit binaries.

