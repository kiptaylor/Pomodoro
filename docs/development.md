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

- The repository ignores build outputs (`bin/`, `obj/`).
- `dist/Pomodoro.exe` is intentionally tracked for `v0.1.0` so the repo contains a prebuilt binary.

