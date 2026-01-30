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
dotnet build src/PomodoroTray/PomodoroTray.csproj -c Release
```

Publish a single EXE:

```powershell
$env:DOTNET_CLI_HOME = (Resolve-Path .dotnet_cli_home).Path
dotnet publish src/PomodoroTray/PomodoroTray.csproj -c Release -r win-x64 /p:PublishSingleFile=true /p:SelfContained=true
```

Output:

`src/PomodoroTray/bin/Release/net9.0-windows/win-x64/publish/pom.exe`

