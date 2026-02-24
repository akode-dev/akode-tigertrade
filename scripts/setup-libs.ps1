param(
    [string]$TigerTradePath
)

$ErrorActionPreference = "Stop"

$requiredDlls = @(
    "TigerTrade.Chart.dll",
    "TigerTrade.Core.dll",
    "TigerTrade.Dx.dll",
    "TigerTrade.Sockets.dll",
    "TigerTrade.Tc.dll"
)

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$libsPath = Join-Path $repoRoot "libs"

if (-not (Test-Path $libsPath)) {
    New-Item -Path $libsPath -ItemType Directory -Force | Out-Null
}

$candidates = @()
if ($TigerTradePath) {
    $candidates += $TigerTradePath
}

$candidates += @(
    "$env:ProgramFiles(x86)\TigerTrade",
    "$env:ProgramFiles\TigerTrade",
    "$env:LOCALAPPDATA\Programs\TigerTrade",
    "$env:LOCALAPPDATA\TigerTrade",
    "$env:USERPROFILE\AppData\Local\TigerTrade"
) | Where-Object { $_ -and $_.Trim().Length -gt 0 }

function Test-TigerTradePath {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        return $false
    }

    foreach ($dll in $requiredDlls) {
        if (-not (Test-Path (Join-Path $Path $dll))) {
            return $false
        }
    }

    return $true
}

$sourcePath = $candidates | Select-Object -Unique | Where-Object { Test-TigerTradePath $_ } | Select-Object -First 1

if (-not $sourcePath) {
    Write-Host "TigerTrade installation was not auto-detected." -ForegroundColor Yellow
    $manualPath = Read-Host "Enter full path to TigerTrade installation folder"

    if (-not $manualPath -or -not (Test-TigerTradePath $manualPath)) {
        Write-Error "Invalid TigerTrade path or required DLLs are missing."
        exit 1
    }

    $sourcePath = $manualPath
}

Write-Host "Using TigerTrade path: $sourcePath" -ForegroundColor Cyan

foreach ($dll in $requiredDlls) {
    $src = Join-Path $sourcePath $dll
    $dst = Join-Path $libsPath $dll
    Copy-Item -Path $src -Destination $dst -Force
    Write-Host "Copied: $dll"
}

Write-Host "Done. DLLs are available in $libsPath" -ForegroundColor Green
