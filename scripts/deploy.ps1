param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [string]$DestinationPath = "$env:USERPROFILE\Documents\TigerTrade\Indicators"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$sourceDll = Join-Path $repoRoot "src\Akode.TigerTrade.Indicators\bin\$Configuration\Akode.TigerTrade.Indicators.dll"

if (-not (Test-Path $sourceDll)) {
    Write-Error "Build artifact not found: $sourceDll"
    Write-Host "Build first: msbuild Akode.TigerTrade.slnx /p:Configuration=$Configuration /p:Platform=\"Any CPU\""
    exit 1
}

if (-not (Test-Path $DestinationPath)) {
    New-Item -Path $DestinationPath -ItemType Directory -Force | Out-Null
}

$destinationDll = Join-Path $DestinationPath "Akode.TigerTrade.Indicators.dll"
Copy-Item -Path $sourceDll -Destination $destinationDll -Force

Write-Host "Deployed: $destinationDll" -ForegroundColor Green
