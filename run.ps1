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

$Helpers = Join-Path $ProjectRoot 'scripts\RunHelpers.psm1'
if (Test-Path $Helpers) { Import-Module $Helpers -Force }

$ShellHostProj = Join-Path $ProjectRoot 'src\BndzFinder.ShellHost\BndzFinder.ShellHost.csproj'
$AppProj = Join-Path $ProjectRoot 'src\BndzFinder.App\BndzFinder.App.csproj'

$AppExeItem = Get-BndzAppExe -ProjectRoot $ProjectRoot -Configuration $Configuration
if ($null -eq $AppExeItem) {
    Write-Error "BndzFinder.App.exe not found after build. Expected under src\BndzFinder.App\bin\$Configuration\...\win-x64\"
}
$AppExe = $AppExeItem.FullName

$missingDlls = Test-BndzWinUiRuntime -ExePath $AppExe
if ($missingDlls.Count -gt 0) {
    Write-Host ""
    Write-Host "ERROR: WinUI runtime DLLs missing next to the App exe:" -ForegroundColor Red
    Write-Host "  $($missingDlls -join ', ')" -ForegroundColor Red
    Write-Host "  Folder: $(Split-Path -Parent $AppExe)" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "Self-contained WinUI requires these DLLs beside BndzFinder.App.exe." -ForegroundColor Yellow
    Write-Host "Fix: delete src\BndzFinder.App\bin and obj, then run .\run.cmd again." -ForegroundColor Yellow
    Write-Host "Or publish portable: pwsh -File '$BuildScript' -Configuration $Configuration -Publish" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "Starting ShellHost (minimize hooks, hotkeys, tray)..." -ForegroundColor Cyan
$hostJob = Start-Process pwsh -ArgumentList @(
    '-NoProfile', '-ExecutionPolicy', 'Bypass', '-Command',
    "Set-Location '$ProjectRoot'; dotnet run --project '$ShellHostProj' -c $Configuration"
) -PassThru -WindowStyle Normal

Start-Sleep -Seconds 3

Write-Host "Starting Bndz-Finder App (dock UI)..." -ForegroundColor Cyan
Write-Host "  $AppExe" -ForegroundColor DarkGray
Write-Host "Look for the dock along the bottom edge of your screen." -ForegroundColor Green
Write-Host "Close both terminal windows to exit." -ForegroundColor Yellow
Write-Host "If nothing appears, check %LOCALAPPDATA%\BndzFinder\startup.log" -ForegroundColor DarkGray
$appProc = Start-Process -FilePath $AppExe -WorkingDirectory (Split-Path -Parent $AppExe) -PassThru
Wait-Process -Id $appProc.Id
$appExit = $appProc.ExitCode

if ($appExit -ne 0) {
    Write-Host ""
    Write-Host "Bndz-Finder App exited with code $appExit." -ForegroundColor Red
    Write-Host (Format-BndzExitCodeHint -ExitCode $appExit) -ForegroundColor Red
    Write-Host "Log: $env:LOCALAPPDATA\BndzFinder\startup.log" -ForegroundColor Yellow
}

if (-not $hostJob.HasExited) {
    Stop-Process -Id $hostJob.Id -Force -ErrorAction SilentlyContinue
}
