#Requires -Version 5.1
<#
.SYNOPSIS
    Full Bndz-Finder build for Windows (WinUI 3 + all projects).
.DESCRIPTION
    Builds the complete solution, optionally publishes a self-contained portable bundle.
    There is NO dist/ folder — output goes to bin/ or bin/Publish/Portable/.
.PARAMETER Configuration
    Debug or Release (default: Release)
.PARAMETER Publish
    Publish self-contained portable build to src/*/bin/Publish/Portable/win-x64
.PARAMETER SkipTests
    Skip running unit tests
#>
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [switch]$Publish,
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$Sln = Join-Path $Root 'BndzFinder.sln'

function Invoke-DotNet {
    param(
        [Parameter(Mandatory = $true, ValueFromRemainingArguments = $true)]
        [string[]]$Arguments
    )

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

function Test-NuGetConnectivity {
    try {
        $resolved = [System.Net.Dns]::GetHostEntry('www.nuget.org')
        if ($resolved.AddressList.Count -gt 0) {
            return $true
        }
    }
    catch {
        return $false
    }

    return $false
}

if (-not (Test-Path $Sln)) {
    Write-Host "ERROR: BndzFinder.sln not found at: $Sln" -ForegroundColor Red
    Write-Host ""
    Write-Host "You are probably in the wrong folder. Try one of these:" -ForegroundColor Yellow
    Write-Host "  cd <repo-root> ; pwsh -File .\build.ps1"
    Write-Host "  cd <repo-root>\BndzFinder ; pwsh -File .\scripts\build.ps1"
    Write-Host ""
    Write-Host "Current script location: $($MyInvocation.MyCommand.Path)" -ForegroundColor DarkGray
    exit 1
}

Write-Host "Project root: $Root" -ForegroundColor DarkGray

if (-not (Test-NuGetConnectivity)) {
    Write-Host "WARNING: Cannot resolve www.nuget.org (DNS/network issue)." -ForegroundColor Yellow
    Write-Host "WinUI projects require Microsoft.WindowsAppSDK from NuGet on first restore." -ForegroundColor Yellow
    Write-Host "Fix connectivity (DNS, firewall, VPN, proxy) then re-run build." -ForegroundColor Yellow
    Write-Host "See BndzFinder/docs/WINDOWS_DEV.md for NU1301 troubleshooting." -ForegroundColor Yellow
    Write-Host ""
}

Write-Host "==> Restoring BndzFinder.sln..." -ForegroundColor Cyan
Invoke-DotNet restore $Sln

Write-Host "==> Building full solution ($Configuration)..." -ForegroundColor Cyan
Invoke-DotNet build $Sln -c $Configuration --no-restore

if (-not $SkipTests) {
    Write-Host "==> Running tests..." -ForegroundColor Cyan
    Invoke-DotNet test $Sln -c $Configuration --no-build
}

if ($Publish) {
    $AppOut = Join-Path $Root "src\BndzFinder.App\bin\Publish\Portable\win-x64"
    $HostOut = Join-Path $Root "src\BndzFinder.ShellHost\bin\Publish\Portable\win-x64"

    Write-Host "==> Publishing BndzFinder.App to $AppOut..." -ForegroundColor Cyan
    Invoke-DotNet publish (Join-Path $Root "src\BndzFinder.App\BndzFinder.App.csproj") `
        -c $Configuration `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:WindowsAppSDKSelfContained=true `
        -o $AppOut

    Write-Host "==> Publishing BndzFinder.ShellHost to $HostOut..." -ForegroundColor Cyan
    Invoke-DotNet publish (Join-Path $Root "src\BndzFinder.ShellHost\BndzFinder.ShellHost.csproj") `
        -c $Configuration `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -o $HostOut

    Write-Host ""
    Write-Host "Published bundle:" -ForegroundColor Green
    Write-Host "  App:       $AppOut\BndzFinder.App.exe"
    Write-Host "  ShellHost: $HostOut\BndzFinder.ShellHost.exe"
    Write-Host ""
    Write-Host "Run portable (start ShellHost first, then App):" -ForegroundColor Yellow
    Write-Host "  Start-Process `"$HostOut\BndzFinder.ShellHost.exe`""
    Write-Host "  Start-Process `"$AppOut\BndzFinder.App.exe`""
}

Write-Host "==> Build complete." -ForegroundColor Green
Write-Host ""
Write-Host "Dev run (two terminals):" -ForegroundColor Yellow
Write-Host "  dotnet run --project $Root\src\BndzFinder.ShellHost\BndzFinder.ShellHost.csproj -c $Configuration"
Write-Host "  dotnet run --project $Root\src\BndzFinder.App\BndzFinder.App.csproj -c $Configuration"
Write-Host ""
Write-Host "Or one command from repo root:" -ForegroundColor Yellow
Write-Host "  pwsh -File .\run.ps1"
