#Requires -Version 5.1
<#
.SYNOPSIS
    Quick pre-build checks before running .\run.cmd
#>
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $Root

Write-Host "Bndz-Finder preflight" -ForegroundColor Cyan
Write-Host "Project root: $Root" -ForegroundColor DarkGray
Write-Host ""

$ok = $true

$sdk = (dotnet --version).Trim()
Write-Host "dotnet --version : $sdk"
if ($sdk -match '^9\.0\.') {
    Write-Host "  OK — .NET 9 SDK active" -ForegroundColor Green
}
else {
    Write-Host "  FAIL — need .NET 9 SDK (global.json pins 9.0.300)" -ForegroundColor Red
    Write-Host "  Install: https://dotnet.microsoft.com/download/dotnet/9.0" -ForegroundColor Yellow
    $ok = $false
}

Write-Host ""
Write-Host "Installed SDKs:"
dotnet --list-sdks

$wasdk = Join-Path $env:USERPROFILE '.nuget\packages\microsoft.windowsappsdk\1.6.250108002'
$buildTools = Join-Path $env:USERPROFILE '.nuget\packages\microsoft.windows.sdk.buildtools\10.0.22621.756'
Write-Host ""
if (Test-Path $wasdk) { Write-Host "OK — Windows App SDK cached" -ForegroundColor Green }
else { Write-Host "PENDING — Windows App SDK not cached (first restore downloads it)" -ForegroundColor Yellow }

if (Test-Path $buildTools) { Write-Host "OK — Windows SDK BuildTools cached" -ForegroundColor Green }
else { Write-Host "PENDING — Windows SDK BuildTools not cached" -ForegroundColor Yellow }

try {
    [void][System.Net.Dns]::GetHostEntry('api.nuget.org')
    Write-Host "OK — NuGet reachable" -ForegroundColor Green
}
catch {
    Write-Host "WARN — NuGet unreachable (restore may fail)" -ForegroundColor Yellow
}

Write-Host ""
if ($ok) {
    Write-Host "Preflight passed. Run: .\run.cmd" -ForegroundColor Green
    exit 0
}

Write-Host "Preflight failed. Fix items above before building." -ForegroundColor Red
exit 1
