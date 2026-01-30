# 🍅 Pomodoro (Windows)

<p align="center">
  <strong>A tiny, personal Pomodoro timer with a real window + system tray support.</strong>
</p>

<p align="center">
  <a href="https://github.com/kiptaylor/Pomodoro/actions/workflows/ci.yml"><img src="https://img.shields.io/github/actions/workflow/status/kiptaylor/Pomodoro/ci.yml?branch=main&style=for-the-badge" alt="CI status"></a>
  <a href="https://github.com/kiptaylor/Pomodoro/releases"><img src="https://img.shields.io/github/v/release/kiptaylor/Pomodoro?style=for-the-badge" alt="GitHub release"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-All%20Rights%20Reserved-red?style=for-the-badge" alt="License"></a>
  <img src="https://img.shields.io/badge/Platform-Windows-0078D6?style=for-the-badge" alt="Platform: Windows">
  <img src="https://img.shields.io/badge/.NET-9-512BD4?style=for-the-badge" alt=".NET 9">
</p>

## What it is

This is a small WinForms app that:

- Runs as a normal window (Start/Pause/Resume/Stop)
- Can minimize to the system tray
- Sends notifications when important things happen
- Lets you tweak your session lengths (work/break/long break/cycles)

## Download (recommended)

Grab the latest release from GitHub Releases and run `pom.exe`.

## Docs

See `docs/README.md` for a wiki-style set of pages (install/usage/config/troubleshooting/dev/release).

## Usage

### UI

- Double-click `pom.exe` to open the window.
- Close button or `Esc` prompts:
  - **Yes** → go to tray
  - **No** → exit
  - **Cancel** → keep open

### Tray

- Left-click tray icon: opens the window
- Right-click tray icon: menu (Start/Pause/Resume/Stop/Exit)
- “Went to tray” notification is clickable and opens the window

### CLI (optional)

You can control the running instance from a terminal:

```powershell
pom start
pom pause
pom resume
pom stop
pom open
pom exit
```

## Build

Prereqs: Windows + .NET SDK (tested with .NET 9).

```powershell
$env:DOTNET_CLI_HOME = (Resolve-Path .dotnet_cli_home).Path
dotnet build Pomodoro.sln -c Release
```

## Publish (single EXE)

```powershell
$env:DOTNET_CLI_HOME = (Resolve-Path .dotnet_cli_home).Path
dotnet publish src/PomodoroCore/PomodoroCore.csproj -c Release -o dist
```

Output:

`dist/Pomodoro.exe`

## Data / config location

The app stores state + config under LocalAppData. To print the exact paths:

```powershell
pom where
```

## License

See `LICENSE`.

