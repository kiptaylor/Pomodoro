# Data + Privacy

## What’s stored

The app stores:
- `config.json` (your defaults)
- `state.json` (current session state)
- `log.jsonl` (simple event log)

## Where it’s stored

By default it uses LocalAppData, e.g.:

`%LOCALAPPDATA%\\Pomodoro`

To print the exact locations on your machine:

```powershell
pom where
```

## Secrets

This app does not require any API keys or online accounts.

