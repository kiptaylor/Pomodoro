# Installation

## Option A: Download the EXE (recommended)

1) Go to GitHub Releases for this repo.
2) Download the latest `pom-win-x64.zip`.
3) Extract it and run `pom.exe`.

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
dotnet publish src/PomodoroCore/PomodoroCore.csproj -c Release
```

Output:

`dist/Pomodoro.exe`

