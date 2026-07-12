#Requires -Version 5.1
<#
.SYNOPSIS
    Build and run Bndz-Finder on Windows (ShellHost + WinUI App).
.PARAMETER Publish
    Publish portable build first, then run from Publish folder.
.PARAMETER Configuration
    Debug or Release (default: Release)
#>
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [switch]$Publish
)

$ErrorActionPreference = 'Stop'
$RepoRoot = $PSScriptRoot

# Resolve BndzFinder project root (folder containing BndzFinder.sln)
if (Test-Path (Join-Path $RepoRoot 'BndzFinder\BndzFinder.sln')) {
    $ProjectRoot = Join-Path $RepoRoot 'BndzFinder'
} elseif (Test-Path (Join-Path $RepoRoot 'BndzFinder.sln')) {
    $ProjectRoot = $RepoRoot
} else {
    Write-Error "Cannot find BndzFinder.sln. Run this from the cloned repo root or the inner BndzFinder folder."
}

$BuildScript = Join-Path $ProjectRoot 'scripts\build.ps1'
if (-not (Test-Path $BuildScript)) {
    Write-Error "Missing $BuildScript"
}

if ($Publish) {
    & $BuildScript -Configuration $Configuration -Publish
    $AppExe = Join-Path $ProjectRoot 'src\BndzFinder.App\bin\Publish\Portable\win-x64\BndzFinder.App.exe'
    $HostExe = Join-Path $ProjectRoot 'src\BndzFinder.ShellHost\bin\Publish\Portable\win-x64\BndzFinder.ShellHost.exe'
    if (-not (Test-Path $HostExe)) {
        $HostExe = Join-Path $ProjectRoot 'src\BndzFinder.ShellHost\bin\Publish\Portable\win-x64\BndzFinder.ShellHost.dll'
    }
    Write-Host "Starting ShellHost..." -ForegroundColor Cyan
    if ($HostExe -like '*.dll') {
        Start-Process dotnet -ArgumentList "`"$HostExe`"" -WindowStyle Minimized
    } else {
        Start-Process $HostExe -WindowStyle Minimized
    }
    Start-Sleep -Seconds 2
    Write-Host "Starting App..." -ForegroundColor Cyan
    Start-Process $AppExe
    exit 0
}

& $BuildScript -Configuration $Configuration -SkipTests

$ShellHostProj = Join-Path $ProjectRoot 'src\BndzFinder.ShellHost\BndzFinder.ShellHost.csproj'
$AppProj = Join-Path $ProjectRoot 'src\BndzFinder.App\BndzFinder.App.csproj'

Write-Host ""
Write-Host "Starting ShellHost (minimize hooks, hotkeys, tray)..." -ForegroundColor Cyan
$hostJob = Start-Process pwsh -ArgumentList @(
    '-NoProfile', '-Command',
    "dotnet run --project `"$ShellHostProj`" -c $Configuration"
) -PassThru -WindowStyle Normal

Start-Sleep -Seconds 3

Write-Host "Starting Bndz-Finder App (dock UI)..." -ForegroundColor Cyan
Write-Host "Close both terminal windows to exit." -ForegroundColor Yellow
dotnet run --project $AppProj -c $Configuration

if (-not $hostJob.HasExited) {
    Stop-Process -Id $hostJob.Id -Force -ErrorAction SilentlyContinue
}
