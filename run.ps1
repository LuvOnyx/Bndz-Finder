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

function Invoke-BndzScript {
    param([string]$Path, [hashtable]$Params = @{})
    $args = @()
    foreach ($key in $Params.Keys) {
        if ($Params[$key] -is [switch] -and $Params[$key]) { $args += "-$key" }
        elseif ($Params[$key] -is [string]) { $args += "-$key"; $args += $Params[$key] }
    }
    & pwsh -NoProfile -ExecutionPolicy Bypass -File $Path @args
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

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

$SystemIconsDir = Join-Path $ProjectRoot 'assets\icons\system'
$RequiredIcons = @('finder.png', 'launchpad.png', 'calendar.png', 'trash.png', 'weather.png', 'preferences.png')
$MissingIcons = $RequiredIcons | Where-Object { -not (Test-Path (Join-Path $SystemIconsDir $_)) }
if ($MissingIcons.Count -gt 0) {
    Write-Host ""
    Write-Host "WARNING: Official macosicons.com system icons are not imported yet." -ForegroundColor Yellow
    Write-Host "  Missing: $($MissingIcons -join ', ')" -ForegroundColor Yellow
    Write-Host "  Run: `$env:MACOSICONS_API_KEY='your-key'; pwsh -File '$ProjectRoot\scripts\import-macos-icons.ps1'" -ForegroundColor Yellow
    Write-Host "  Free API key: https://docs.macosicons.com/api-management" -ForegroundColor DarkGray
    Write-Host ""
}

if ($Publish) {
    Invoke-BndzScript $BuildScript @{ Configuration = $Configuration; Publish = $true }
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

Invoke-BndzScript $BuildScript @{ Configuration = $Configuration; SkipTests = $true }

$ShellHostProj = Join-Path $ProjectRoot 'src\BndzFinder.ShellHost\BndzFinder.ShellHost.csproj'
$AppProj = Join-Path $ProjectRoot 'src\BndzFinder.App\BndzFinder.App.csproj'

Write-Host ""
Write-Host "Starting ShellHost (minimize hooks, hotkeys, tray)..." -ForegroundColor Cyan
$hostJob = Start-Process pwsh -ArgumentList @(
    '-NoProfile', '-ExecutionPolicy', 'Bypass', '-Command',
    "Set-Location '$ProjectRoot'; dotnet run --project '$ShellHostProj' -c $Configuration"
) -PassThru -WindowStyle Normal

Start-Sleep -Seconds 3

Write-Host "Starting Bndz-Finder App (dock UI)..." -ForegroundColor Cyan
Write-Host "Close both terminal windows to exit." -ForegroundColor Yellow
Write-Host "If nothing appears, check %LOCALAPPDATA%\BndzFinder\startup.log" -ForegroundColor DarkGray
Push-Location $ProjectRoot
try {
    dotnet run --project $AppProj -c $Configuration
    $appExit = $LASTEXITCODE
}
finally {
    Pop-Location
}

if ($appExit -ne 0) {
    Write-Host ""
    Write-Host "Bndz-Finder App exited with code $appExit." -ForegroundColor Red
    Write-Host "Common causes: another instance running, or a missing dependency." -ForegroundColor Red
    Write-Host "Log: $env:LOCALAPPDATA\BndzFinder\startup.log" -ForegroundColor Yellow
}

if (-not $hostJob.HasExited) {
    Stop-Process -Id $hostJob.Id -Force -ErrorAction SilentlyContinue
}
