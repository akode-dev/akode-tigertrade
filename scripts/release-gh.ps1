param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",

    [string]$Title,
    [string]$NotesFile,

    [switch]$Draft,
    [switch]$Prerelease,
    [switch]$SkipBuild,
    [switch]$AllowDirty
)

$ErrorActionPreference = "Stop"

function Require-Command {
    param([string]$Name)
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' was not found in PATH."
    }
}

function Exec {
    param(
        [string]$FilePath,
        [string[]]$Arguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        $joined = $Arguments -join " "
        throw "Command failed: $FilePath $joined"
    }
}

Require-Command -Name "git"
Require-Command -Name "gh"
Require-Command -Name "dotnet"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $repoRoot

Exec -FilePath "git" -Arguments @("rev-parse", "--is-inside-work-tree")

try {
    Exec -FilePath "git" -Arguments @("remote", "get-url", "origin")
}
catch {
    throw "Git remote 'origin' is not configured."
}

if (-not $AllowDirty) {
    $dirty = git status --porcelain
    if ($dirty) {
        throw "Working tree is not clean. Commit/stash changes or use -AllowDirty."
    }
}

try {
    Exec -FilePath "gh" -Arguments @("auth", "status")
}
catch {
    throw "GitHub CLI is not authenticated. Run: gh auth login"
}

$tag = if ($Version.StartsWith("v")) { $Version } else { "v$Version" }
$releaseTitle = if ($Title) { $Title } else { "Akode TigerTrade Indicators $tag" }

$requiredTigerTradeDlls = @(
    "TigerTrade.Chart.dll",
    "TigerTrade.Core.dll",
    "TigerTrade.Dx.dll"
)

if (-not $SkipBuild) {
    foreach ($dll in $requiredTigerTradeDlls) {
        $depPath = Join-Path $repoRoot "libs\$dll"
        if (-not (Test-Path $depPath)) {
            throw "Missing dependency: $depPath. Run .\scripts\setup-libs.ps1 first."
        }
    }

    Exec -FilePath "dotnet" -Arguments @(
        "msbuild",
        "Akode.TigerTrade.slnx",
        "/p:Configuration=$Configuration",
        "/p:Platform=Any CPU"
    )
}

$artifactPath = Join-Path $repoRoot "src\Akode.TigerTrade.Indicators\bin\$Configuration\Akode.TigerTrade.Indicators.dll"
if (-not (Test-Path $artifactPath)) {
    throw "Build artifact not found: $artifactPath"
}

$hash = (Get-FileHash -Path $artifactPath -Algorithm SHA256).Hash.ToLowerInvariant()
$checksumPath = Join-Path (Split-Path $artifactPath -Parent) "Akode.TigerTrade.Indicators.dll.sha256"
"$hash  Akode.TigerTrade.Indicators.dll" | Set-Content -Path $checksumPath

$localTagExists = $false
git rev-parse -q --verify "refs/tags/$tag" *> $null
if ($LASTEXITCODE -eq 0) {
    $localTagExists = $true
}

if (-not $localTagExists) {
    Exec -FilePath "git" -Arguments @("tag", "-a", $tag, "-m", "Release $tag")
}

$remoteTagExists = $false
git ls-remote --exit-code --tags origin "refs/tags/$tag" *> $null
if ($LASTEXITCODE -eq 0) {
    $remoteTagExists = $true
}

if (-not $remoteTagExists) {
    Exec -FilePath "git" -Arguments @("push", "origin", "refs/tags/$tag")
}

gh release view $tag *> $null
if ($LASTEXITCODE -eq 0) {
    throw "GitHub release '$tag' already exists."
}

$releaseArgs = @(
    "release",
    "create",
    $tag,
    $artifactPath,
    $checksumPath,
    "--title",
    $releaseTitle
)

if ($Draft) {
    $releaseArgs += "--draft"
}

if ($Prerelease) {
    $releaseArgs += "--prerelease"
}

if ($NotesFile) {
    if (-not (Test-Path $NotesFile)) {
        throw "Notes file not found: $NotesFile"
    }
    $releaseArgs += @("--notes-file", (Resolve-Path $NotesFile))
}
else {
    $releaseArgs += "--generate-notes"
}

Exec -FilePath "gh" -Arguments $releaseArgs

Write-Host "Release published: $tag" -ForegroundColor Green
Write-Host "Uploaded assets:" -ForegroundColor Green
Write-Host " - $artifactPath"
Write-Host " - $checksumPath"
