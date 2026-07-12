#Requires -Version 5.1
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [switch]$Publish
)

$ErrorActionPreference = 'Stop'
$ProjectRoot = $PSScriptRoot

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

$ShellHostProj = Join-Path $ProjectRoot 'src\BndzFinder.ShellHost\BndzFinder.ShellHost.csproj'
$AppProj = Join-Path $ProjectRoot 'src\BndzFinder.App\BndzFinder.App.csproj'

Write-Host "Starting ShellHost..." -ForegroundColor Cyan
$hostJob = Start-Process pwsh -ArgumentList @(
    '-NoProfile', '-Command',
    "dotnet run --project `"$ShellHostProj`" -c $Configuration"
) -PassThru

Start-Sleep -Seconds 3
Write-Host "Starting App..." -ForegroundColor Cyan
dotnet run --project $AppProj -c $Configuration

if (-not $hostJob.HasExited) {
    Stop-Process -Id $hostJob.Id -Force -ErrorAction SilentlyContinue
}
