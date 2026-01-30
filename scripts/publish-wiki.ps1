[CmdletBinding()]
param(
  # Repo URL, like: https://github.com/kiptaylor/Pomodoro or https://github.com/kiptaylor/Pomodoro.git
  [Parameter(Mandatory = $false)]
  [string]$Repo = "https://github.com/kiptaylor/Pomodoro",

  [Parameter(Mandatory = $false)]
  [string]$DocsDir = "docs"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Write-FileUtf8NoBom([string]$Path, [string]$Content) {
  $dir = Split-Path -Parent $Path
  if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
  $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
  [System.IO.File]::WriteAllText($Path, $Content, $utf8NoBom)
}

function Copy-DocToWiki([string]$RepoPath, [string]$DocPath, [string]$WikiFileName) {
  if (-not (Test-Path $DocPath)) { throw "Missing doc: $DocPath" }
  $content = Get-Content -Raw $DocPath
  Write-FileUtf8NoBom (Join-Path $RepoPath $WikiFileName) $content
}

function Get-WikiRepoUrl([string]$RepoUrl) {
  $base = $RepoUrl.Trim()
  if ($base.EndsWith(".git")) { $base = $base.Substring(0, $base.Length - 4) }
  $base = $base.TrimEnd("/")
  return "$base.wiki.git"
}

$wikiRepo = Get-WikiRepoUrl $Repo

if (-not (Test-Path $DocsDir)) {
  throw "Docs directory not found: $DocsDir"
}

$tmp = Join-Path $env:TEMP ("pomodoro_wiki_" + [Guid]::NewGuid().ToString("N"))

try {
  Write-Host "Cloning wiki repo: $wikiRepo"
  git clone $wikiRepo $tmp | Out-Host

  if ($LASTEXITCODE -ne 0) {
    throw "git clone failed"
  }
} catch {
  Write-Host ""
  Write-Host "Failed to clone the wiki repo."
  Write-Host "This usually means the GitHub Wiki feature is disabled (or the wiki has never been created yet)."
  Write-Host ""
  Write-Host "Fix:"
  Write-Host "1) On GitHub: Repo → Settings → General → Features → enable 'Wikis'"
  Write-Host "2) Visit: Repo → Wiki and create the first page (Home) if prompted"
  Write-Host ""
  Write-Host "Then re-run:"
  Write-Host "  .\\scripts\\publish-wiki.ps1"
  throw
}

try {
  $pages = @(
    @{ Doc = "$DocsDir/installation.md";     Wiki = "Installation.md" },
    @{ Doc = "$DocsDir/usage.md";            Wiki = "Usage.md" },
    @{ Doc = "$DocsDir/tray-ui.md";          Wiki = "Tray-UI.md" },
    @{ Doc = "$DocsDir/cli.md";              Wiki = "CLI.md" },
    @{ Doc = "$DocsDir/configuration.md";    Wiki = "Configuration.md" },
    @{ Doc = "$DocsDir/data-privacy.md";     Wiki = "Data-Privacy.md" },
    @{ Doc = "$DocsDir/troubleshooting.md";  Wiki = "Troubleshooting.md" },
    @{ Doc = "$DocsDir/development.md";      Wiki = "Development.md" },
    @{ Doc = "$DocsDir/releasing.md";        Wiki = "Releasing.md" }
  )

  $homeContent =
@"
# Pomodoro Wiki

- [Installation](Installation)
- [Usage](Usage)
- [Tray + Window UI](Tray-UI)
- [CLI Commands](CLI)
- [Configuration](Configuration)
- [Data + Privacy](Data-Privacy)
- [Troubleshooting](Troubleshooting)
- [Development](Development)
- [Releasing](Releasing)

Source docs live in the repo under `docs/`.
"@

  $sidebarContent =
@"
### Pomodoro

- [Home](Home)
- [Installation](Installation)
- [Usage](Usage)
- [Tray + Window UI](Tray-UI)
- [CLI Commands](CLI)
- [Configuration](Configuration)
- [Data + Privacy](Data-Privacy)
- [Troubleshooting](Troubleshooting)
- [Development](Development)
- [Releasing](Releasing)
"@

  Write-FileUtf8NoBom (Join-Path $tmp "Home.md") $homeContent
  Write-FileUtf8NoBom (Join-Path $tmp "_Sidebar.md") $sidebarContent

  foreach ($p in $pages) {
    Copy-DocToWiki -RepoPath $tmp -DocPath $p.Doc -WikiFileName $p.Wiki
  }

  $status = git -C $tmp status --porcelain
  if (-not $status) {
    Write-Host "Wiki already up to date."
    return
  }

  git -C $tmp add -A | Out-Host
  git -C $tmp commit -m "Update wiki" --no-gpg-sign | Out-Host
  git -C $tmp push | Out-Host
  Write-Host "Wiki updated."
}
finally {
  if (Test-Path $tmp) { Remove-Item -Recurse -Force $tmp }
}
