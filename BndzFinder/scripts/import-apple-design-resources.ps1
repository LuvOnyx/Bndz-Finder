#Requires -Version 7.0
<#
.SYNOPSIS
    Stage Apple Design Resources and macosicons dock templates for Bndz-Finder.
.DESCRIPTION
    - Documents manual steps for macOS UI Kit (Figma/Sketch via developer.apple.com)
    - Optionally downloads SF Pro fonts from Apple's public CDN
    - Accepts user-provided exports for app icon shell and dock reference PNGs
.PARAMETER ImportSfPro
    Download and extract SF Pro from Apple's public DMG (large download).
.PARAMETER AppIconShellPath
    Path to a 1024x1024 app icon shell PNG exported from Apple's App Icon Template.
.PARAMETER DockReferencePath
    Path to dock chrome reference PNG (macosicons "macOS dock template" or Apple UI kit export).
#>
param(
    [switch]$ImportSfPro,
    [string]$AppIconShellPath,
    [string]$DockReferencePath
)

$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$TemplateDir = Join-Path $Root 'assets\templates\apple-design-resources'
$FontDir = Join-Path $Root 'assets\fonts'
New-Item -ItemType Directory -Force -Path $TemplateDir, $FontDir | Out-Null

Write-Host @"

Apple Design Resources setup
============================
Official kits (manual — Apple distributes via Figma/Sketch libraries):
  https://developer.apple.com/design/resources/

macOS dock + icon creation templates (manual download):
  https://macosicons.com/resources
  - macOS
  - macOS dock template

Bndz-Finder uses:
  - WinUI glass dock bar (DockBarControl) — NOT a squircle SVG as the dock
  - app-icon-shell.png for icon gloss overlay in MacStyleIconPipeline
  - dock-reference.png for layout tuning only (not rendered directly)

"@ -ForegroundColor Cyan

if ($AppIconShellPath) {
    if (-not (Test-Path $AppIconShellPath)) { Write-Error "File not found: $AppIconShellPath" }
    Copy-Item $AppIconShellPath (Join-Path $TemplateDir 'app-icon-shell.png') -Force
    Write-Host "Copied app icon shell template." -ForegroundColor Green
}

if ($DockReferencePath) {
    if (-not (Test-Path $DockReferencePath)) { Write-Error "File not found: $DockReferencePath" }
    Copy-Item $DockReferencePath (Join-Path $TemplateDir 'dock-reference.png') -Force
    Write-Host "Copied dock reference." -ForegroundColor Green
}

if ($ImportSfPro) {
    $dmgUrl = 'https://devimages-cdn.apple.com/design/resources/download/SF-Pro.dmg'
    $tempDmg = Join-Path $env:TEMP 'SF-Pro.dmg'
    Write-Host "Downloading SF Pro (~200 MB)..." -ForegroundColor Yellow
    Invoke-WebRequest -Uri $dmgUrl -OutFile $tempDmg

    if ($IsWindows) {
        Write-Warning "Mount/extract SF-Pro.dmg on Windows requires 7-Zip or manual extract. Copy .otf files to: $FontDir"
    }
    else {
        $mount = Join-Path $env:TEMP 'sf-pro-mount'
        New-Item -ItemType Directory -Force -Path $mount | Out-Null
        hdiutil attach $tempDmg -mountpoint $mount -quiet
        Get-ChildItem -Path $mount -Recurse -Filter '*.otf' | Copy-Item -Destination $FontDir -Force
        hdiutil detach $mount -quiet
        Write-Host "SF Pro fonts copied to $FontDir" -ForegroundColor Green
    }
}

$shellPath = Join-Path $TemplateDir 'app-icon-shell.png'
if (-not (Test-Path $shellPath)) {
    Write-Warning @"
app-icon-shell.png not found.
Export from Apple's App Icon Template (Photoshop/Illustrator) at:
  https://developer.apple.com/design/resources/
Then run:
  pwsh -File BndzFinder/scripts/import-apple-design-resources.ps1 -AppIconShellPath 'C:\path\to\shell.png'
"@
}

Write-Host "Template directory: $TemplateDir" -ForegroundColor Cyan
