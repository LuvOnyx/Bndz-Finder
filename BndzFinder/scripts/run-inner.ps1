#Requires -Version 5.1
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [switch]$Publish
)

$ErrorActionPreference = 'Stop'
$ProjectRoot = Split-Path -Parent $PSScriptRoot

if ($Publish) {
    & (Join-Path $ProjectRoot 'scripts\build.ps1') -Configuration $Configuration -Publish
    $AppExe = Join-Path $ProjectRoot 'src\BndzFinder.App\bin\Publish\Portable\win-x64\BndzFinder.App.exe'
    $HostExe = Join-Path $ProjectRoot 'src\BndzFinder.ShellHost\bin\Publish\Portable\win-x64\BndzFinder.ShellHost.exe'
    Start-Process $HostExe -WindowStyle Minimized
    Start-Sleep -Seconds 2
    Start-Process $AppExe
    exit 0
}

& (Join-Path $ProjectRoot 'scripts\build.ps1') -Configuration $Configuration -SkipTests

Import-Module (Join-Path $ProjectRoot 'scripts\RunHelpers.psm1') -Force

$ShellHostProj = Join-Path $ProjectRoot 'src\BndzFinder.ShellHost\BndzFinder.ShellHost.csproj'
$AppExeItem = Get-BndzAppExe -ProjectRoot $ProjectRoot -Configuration $Configuration
if ($null -eq $AppExeItem) {
    Write-Error "BndzFinder.App.exe not found after build."
}
$AppExe = $AppExeItem.FullName

Write-Host "Starting ShellHost..." -ForegroundColor Cyan
$hostJob = Start-Process pwsh -ArgumentList @(
    '-NoProfile', '-Command',
    "Set-Location '$ProjectRoot'; dotnet run --project `"$ShellHostProj`" -c $Configuration"
) -PassThru

Start-Sleep -Seconds 3
Write-Host "Starting App: $AppExe" -ForegroundColor Cyan
Push-Location (Split-Path -Parent $AppExe)
try {
    & $AppExe
    $appExit = $LASTEXITCODE
}
finally {
    Pop-Location
}

if ($appExit -ne 0) {
    Write-Host (Format-BndzExitCodeHint -ExitCode $appExit) -ForegroundColor Red
    Write-Host "Log: $env:LOCALAPPDATA\BndzFinder\startup.log" -ForegroundColor Yellow
}

if (-not $hostJob.HasExited) {
    Stop-Process -Id $hostJob.Id -Force -ErrorAction SilentlyContinue
}
