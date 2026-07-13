#Requires -Version 7.0
<#
.SYNOPSIS
    Generate bundled macOS-style assets (system icons, default theme pack) without API keys.
#>
param(
    [string]$ThemesRoot = (Join-Path $env:APPDATA 'BndzFinder\themes'),
    [string]$IconsRoot = (Join-Path (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)) 'assets\icons\system')
)

$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

Write-Host "Generating bundled assets via BndzFinder.Shell (SkiaSharp)..." -ForegroundColor Cyan

# Runtime bootstrap generates assets; this script documents paths and ensures directories exist.
New-Item -ItemType Directory -Force -Path $ThemesRoot, $IconsRoot | Out-Null

Write-Host @"

Bundled asset locations
=======================
System icons (generated on first app run):
  $IconsRoot

Default theme pack (generated on first app run):
  $ThemesRoot\sequoia-default\

To apply SF Pro fonts (optional):
  pwsh -File $Root\scripts\import-apple-design-resources.ps1 -ImportSfPro

To import official Apple app icon shell PNG (optional):
  pwsh -File $Root\scripts\import-apple-design-resources.ps1 -AppIconShellPath 'C:\path\to\shell.png'

"@ -ForegroundColor Green
