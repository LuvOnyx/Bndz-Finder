#Requires -Version 5.1
<#
.SYNOPSIS
    Full Bndz-Finder build for Windows (WinUI 3 + all projects).
.DESCRIPTION
    Builds the complete solution, optionally publishes a self-contained portable exe.
.PARAMETER Configuration
    Debug or Release (default: Release)
.PARAMETER Publish
    Publish self-contained portable build to bin/Publish/Portable
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

Write-Host "==> Restoring BndzFinder.sln..." -ForegroundColor Cyan
dotnet restore "$Root\BndzFinder.sln"

Write-Host "==> Building full solution ($Configuration)..." -ForegroundColor Cyan
dotnet build "$Root\BndzFinder.sln" -c $Configuration --no-restore

if (-not $SkipTests) {
    Write-Host "==> Running tests..." -ForegroundColor Cyan
    dotnet test "$Root\BndzFinder.sln" -c $Configuration --no-build
}

if ($Publish) {
    $OutDir = "$Root\src\BndzFinder.App\bin\Publish\Portable\win-x64"
    Write-Host "==> Publishing portable build to $OutDir..." -ForegroundColor Cyan
    dotnet publish "$Root\src\BndzFinder.App\BndzFinder.App.csproj" `
        -c $Configuration `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:WindowsAppSDKSelfContained=true `
        -o $OutDir
    Write-Host "Published: $OutDir\BndzFinder.App.exe" -ForegroundColor Green
}

Write-Host "==> Build complete." -ForegroundColor Green
Write-Host ""
Write-Host "Run:" -ForegroundColor Yellow
Write-Host "  dotnet run --project $Root\src\BndzFinder.App\BndzFinder.App.csproj"
Write-Host "  dotnet run --project $Root\src\BndzFinder.ShellHost\BndzFinder.ShellHost.csproj"
