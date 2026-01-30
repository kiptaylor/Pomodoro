# Installation

## Option A: Run the prebuilt EXE from this repo (v0.1.0)

1) Download/clone this repo.
2) Run `dist/Pomodoro.exe`.

Optional: rename `Pomodoro.exe` → `pom.exe` if you want a shorter command name.

## Option B: Build from source

Requirements:
- Windows
- .NET SDK (tested with .NET 9)

Build:

```powershell
$env:DOTNET_CLI_HOME = (Resolve-Path .dotnet_cli_home).Path
dotnet build Pomodoro.sln -c Release
```

Publish a single EXE:

```powershell
$env:DOTNET_CLI_HOME = (Resolve-Path .dotnet_cli_home).Path
dotnet publish src/PomodoroCore/PomodoroCore.csproj -c Release -o dist
```

Output:

`dist/Pomodoro.exe`

