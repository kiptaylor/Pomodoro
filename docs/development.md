# Development

## Solution layout

- `src/PomodoroCore/` — WinForms app (UI + tray + IPC) + shared core models/persistence
- `src/PomodoroCore/Tray/` — tray + window UI sources

## Build

```powershell
$env:DOTNET_CLI_HOME = (Resolve-Path .dotnet_cli_home).Path
dotnet build Pomodoro.sln -c Release
```

## Notes

- The repository ignores build outputs and `pom.exe` so you don’t accidentally commit binaries.

