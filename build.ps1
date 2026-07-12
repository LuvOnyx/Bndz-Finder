#Requires -Version 5.1
<#
.SYNOPSIS
    Repo-root entry point for Bndz-Finder Windows build.
.DESCRIPTION
    Works whether you cloned the full repo or only the inner BndzFinder folder.
    Forwards all parameters to the real build script.
#>
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [switch]$Publish,
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'
$RepoRoot = $PSScriptRoot

$candidates = @(
    (Join-Path $RepoRoot 'BndzFinder\scripts\build.ps1'),
    (Join-Path $RepoRoot 'scripts\build.ps1')
)

$script = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $script) {
    Write-Host "Could not find build.ps1. Checked:" -ForegroundColor Red
    $candidates | ForEach-Object { Write-Host "  $_" }
    Write-Host ""
    Write-Host "From repo root (README + BndzFinder subfolder):" -ForegroundColor Yellow
    Write-Host "  pwsh -File .\build.ps1"
    Write-Host "  pwsh -File .\BndzFinder\scripts\build.ps1"
    Write-Host ""
    Write-Host "From inner BndzFinder folder (BndzFinder.sln here):" -ForegroundColor Yellow
    Write-Host "  pwsh -File .\scripts\build.ps1"
    exit 1
}

Write-Host "Using: $script" -ForegroundColor DarkGray
& pwsh -NoProfile -ExecutionPolicy Bypass -File $script @PSBoundParameters
exit $LASTEXITCODE
