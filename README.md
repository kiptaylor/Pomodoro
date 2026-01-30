# Pomodoro

Tiny Windows-first Pomodoro with a tray app + a CLI.

`pom.exe` is the all-in-one app:
- Double-click: opens the UI; closing asks tray vs exit.
- Terminal: `pom <command>` controls the running instance (or starts it in tray mode automatically).

## Build

From this repo root:

```powershell
$env:DOTNET_CLI_HOME = (Resolve-Path .dotnet_cli_home).Path
dotnet build src/PomodoroTray/PomodoroTray.csproj -c Release
```

## Publish single EXEs

```powershell
$env:DOTNET_CLI_HOME = (Resolve-Path .dotnet_cli_home).Path
dotnet publish src/PomodoroTray/PomodoroTray.csproj -c Release -r win-x64 /p:PublishSingleFile=true /p:SelfContained=true
```

Output:
- `src/PomodoroTray/bin/Release/net9.0-windows/win-x64/publish/pom.exe`

## Usage

Double-click `pom.exe`. A small window opens; press `Esc` or click the close button to choose **tray** vs **exit**.

From any terminal (it will start the background instance if needed):

```powershell
pom start
pom status
pom pause
pom resume
pom stop
pom open
pom exit
```

Config:

```powershell
pom config
pom config set work 30
pom config set break 5
pom config set long 15
pom config set cycles 4
pom config set auto true
pom config set popup true
pom config set sound true
```

State/config are stored under LocalAppData. Run `pom where` to see paths.

## License

See `LICENSE`.
