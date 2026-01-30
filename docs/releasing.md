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
New-Item -ItemType Directory -Force dist | Out-Null
Copy-Item -Force dist\Pomodoro.exe dist\pom.exe
Compress-Archive -Force -Path dist\pom.exe -DestinationPath dist\pom-win-x64.zip
```

4) Create a GitHub Release and upload `pom-win-x64.zip`.

