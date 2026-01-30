# Releasing

## Suggested process

1) Make sure `main` is clean and builds.
2) Publish a Windows build:

```powershell
$env:DOTNET_CLI_HOME = (Resolve-Path .dotnet_cli_home).Path
dotnet publish src/PomodoroCore/PomodoroCore.csproj -c Release -o dist
```

3) Zip the EXE:

```powershell
Compress-Archive -Force -Path dist\Pomodoro.exe -DestinationPath dist\pomodoro-win-x64.zip
```

4) Create a GitHub Release and upload `pomodoro-win-x64.zip`.

