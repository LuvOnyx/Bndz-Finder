#Requires -Version 5.1
<#
.SYNOPSIS
    Full Bndz-Finder build for Windows (WinUI 3 + all projects).
.DESCRIPTION
    Builds the complete solution, optionally publishes a self-contained portable bundle.
    There is NO dist/ folder — output goes to bin/ or bin/Publish/Portable/.
    Restore automatically retries on transient network errors and resumes from the
    local NuGet cache (already-downloaded packages are not re-fetched).
.PARAMETER Configuration
    Debug or Release (default: Release)
.PARAMETER Publish
    Publish self-contained portable build to src/*/bin/Publish/Portable/win-x64
.PARAMETER SkipTests
    Skip running unit tests
.PARAMETER NetworkRetries
    How many times to retry restore on transient WiFi/NuGet failures (default: 15)
#>
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [switch]$Publish,
    [switch]$SkipTests,
    [int]$NetworkRetries = 15
)

$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$Sln = Join-Path $Root 'BndzFinder.sln'

function Test-NuGetConnectivity {
    foreach ($hostName in @('www.nuget.org', 'api.nuget.org')) {
        try {
            $resolved = [System.Net.Dns]::GetHostEntry($hostName)
            if ($resolved.AddressList.Count -gt 0) {
                return $true
            }
        }
        catch {
            continue
        }
    }

    return $false
}

function Test-WindowsAppSdkCached {
    $packageRoot = Join-Path $env:USERPROFILE '.nuget\packages\microsoft.windowsappsdk\1.6.250108002'
    return Test-Path $packageRoot
}

function Test-TransientNetworkError {
    param([string]$Output)

    $patterns = @(
        'NU1301',
        'NU1302',
        'No such host is known',
        'Unable to load the service index',
        'Connection refused',
        'timed out',
        'timeout',
        'A connection attempt failed',
        'The SSL connection could not be established',
        '503',
        '502',
        '504',
        'host is down',
        'network is unreachable'
    )

    foreach ($pattern in $patterns) {
        if ($Output -like "*$pattern*") {
            return $true
        }
    }

    return $false
}

function Wait-ForNuGetConnectivity {
    param(
        [int]$MaxWaitSeconds = 1800,
        [string]$Reason = 'Waiting for NuGet connectivity'
    )

    if (Test-NuGetConnectivity) {
        return $true
    }

    Write-Host "$Reason (Ctrl+C to cancel)..." -ForegroundColor Yellow
    $waited = 0
    while ($waited -lt $MaxWaitSeconds) {
        Start-Sleep -Seconds 5
        $waited += 5
        if (Test-NuGetConnectivity) {
            Write-Host "Network is back — continuing." -ForegroundColor Green
            return $true
        }

        if ($waited % 30 -eq 0) {
            Write-Host "  still offline (${waited}s)..." -ForegroundColor DarkGray
        }
    }

    return $false
}

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

function Invoke-DotNetWithRetry {
    param(
        [string]$Label,
        [int]$MaxAttempts = 1,
        [int]$InitialDelaySeconds = 5,
        [Parameter(Mandatory = $true, ValueFromRemainingArguments = $true)]
        [string[]]$Arguments
    )

    $attempt = 0
    $delay = $InitialDelaySeconds

    while ($true) {
        $attempt++
        if ($MaxAttempts -gt 1) {
            Write-Host "==> $Label (attempt $attempt of $MaxAttempts)..." -ForegroundColor Cyan
        }
        else {
            Write-Host "==> $Label..." -ForegroundColor Cyan
        }

        $captured = New-Object System.Collections.Generic.List[string]
        & dotnet @Arguments 2>&1 | ForEach-Object {
            $line = "$_"
            $captured.Add($line)
            Write-Host $line
        }

        if ($LASTEXITCODE -eq 0) {
            return
        }

        $text = ($captured -join [Environment]::NewLine)
        if ($attempt -ge $MaxAttempts -or -not (Test-TransientNetworkError $text)) {
            exit $LASTEXITCODE
        }

        Write-Host ""
        Write-Host "Transient network issue during $Label." -ForegroundColor Yellow
        Write-Host "NuGet keeps partial downloads in %USERPROFILE%\.nuget\packages — retry will resume, not restart from zero." -ForegroundColor DarkGray

        [void](Wait-ForNuGetConnectivity -MaxWaitSeconds 600 -Reason 'Pausing until NuGet is reachable again')

        Write-Host "Retrying in ${delay}s..." -ForegroundColor Yellow
        Start-Sleep -Seconds $delay
        $delay = [Math]::Min([int]($delay * 1.5), 90)
    }
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
    if (Test-WindowsAppSdkCached) {
        Write-Host "WARNING: NuGet is unreachable — will try cached packages and retry on blips." -ForegroundColor Yellow
        Write-Host ""
    }
    else {
        Write-Host "NuGet is offline and Windows App SDK is not fully cached yet." -ForegroundColor Yellow
        Write-Host "First restore downloads ~500MB+. The script will wait for your connection and auto-retry." -ForegroundColor Yellow
        Write-Host ""
        if (-not (Wait-ForNuGetConnectivity -MaxWaitSeconds 1800 -Reason 'Waiting for internet before first restore')) {
            Write-Host "ERROR: Still offline after 30 minutes. Re-run .\build.cmd when WiFi is stable." -ForegroundColor Red
            exit 1
        }
        Write-Host ""
    }
}

Write-Host "Restore uses resilient retries ($NetworkRetries attempts) — brief WiFi drops should resume automatically." -ForegroundColor DarkGray
Write-Host ""

Invoke-DotNetWithRetry -Label 'Restoring BndzFinder.sln' -MaxAttempts $NetworkRetries -InitialDelaySeconds 8 restore $Sln

Invoke-DotNetWithRetry -Label "Building full solution ($Configuration)" -MaxAttempts 3 -InitialDelaySeconds 5 build $Sln -c $Configuration --no-restore

if (-not $SkipTests) {
    Invoke-DotNet test $Sln -c $Configuration --no-build
}

if ($Publish) {
    $AppOut = Join-Path $Root "src\BndzFinder.App\bin\Publish\Portable\win-x64"
    $HostOut = Join-Path $Root "src\BndzFinder.ShellHost\bin\Publish\Portable\win-x64"

    Invoke-DotNetWithRetry -Label "Publishing BndzFinder.App to $AppOut" -MaxAttempts 5 -InitialDelaySeconds 10 `
        publish (Join-Path $Root "src\BndzFinder.App\BndzFinder.App.csproj") `
        -c $Configuration `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:WindowsAppSDKSelfContained=true `
        -o $AppOut

    Invoke-DotNetWithRetry -Label "Publishing BndzFinder.ShellHost to $HostOut" -MaxAttempts 5 -InitialDelaySeconds 10 `
        publish (Join-Path $Root "src\BndzFinder.ShellHost\BndzFinder.ShellHost.csproj") `
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
