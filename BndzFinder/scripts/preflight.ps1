#Requires -Version 5.1
<#
.SYNOPSIS
    Validates MSBuild wiring before a full Windows build.
#>
param(
    [string]$Root = (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path))
)

$ErrorActionPreference = 'Stop'
$failures = New-Object System.Collections.Generic.List[string]

function Assert-FileContains {
    param([string]$Path, [string]$Pattern, [string]$Message)
    if (-not (Test-Path $Path)) {
        $failures.Add("Missing file: $Path — $Message")
        return
    }
    $content = Get-Content -Raw -Path $Path
    if ($content -notmatch $Pattern) {
        $failures.Add("$Message ($Path)")
    }
}

$windowsProps = Join-Path $Root 'Directory.Build.Windows.props'
$buildTargets = Join-Path $Root 'Directory.Build.targets'
$packagesProps = Join-Path $Root 'Directory.Packages.props'

Assert-FileContains $windowsProps 'MrtCoreEnablePriGeneration' 'PRI generation must be disabled for WinUI class libraries'
Assert-FileContains $buildTargets 'Directory\.Build\.Windows\.targets' 'Directory.Build.targets must import Windows targets shim'
Assert-FileContains $packagesProps 'CommunityToolkit\.Mvvm' 'Central package management must define CommunityToolkit.Mvvm'

$winUiProjects = Get-ChildItem -Path (Join-Path $Root 'src') -Filter '*.csproj' -Recurse |
    Where-Object {
        $text = Get-Content -Raw $_.FullName
        $text -match 'Directory\.Build\.Windows\.props'
    }

foreach ($project in $winUiProjects) {
    $text = Get-Content -Raw $project.FullName
    if (($text | Select-String -Pattern 'CommunityToolkit\.Mvvm' -AllMatches).Matches.Count -gt 0) {
        $failures.Add("Duplicate CommunityToolkit.Mvvm in $($project.Name) — remove per-project reference (use Directory.Build.Windows.props)")
    }
}

if ($failures.Count -gt 0) {
    Write-Host 'Preflight FAILED:' -ForegroundColor Red
    foreach ($item in $failures) {
        Write-Host "  - $item" -ForegroundColor Yellow
    }
    exit 1
}

Write-Host 'Preflight OK — MSBuild wiring and package references look correct.' -ForegroundColor Green
