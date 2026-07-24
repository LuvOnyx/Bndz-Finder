#Requires -Version 7.0
<#
.SYNOPSIS
    Import Figma-exported PNGs into BndzFinder/assets/figma and sync system icons.
.PARAMETER SourceZip
    Zip of exports (any nested folders OK — files matched by name).
.PARAMETER SourceDir
    Folder of exports (searched recursively by filename).
#>
param(
    [string]$SourceZip,
    [string]$SourceDir
)

$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$FigmaRoot = Join-Path $Root 'assets\figma'
$SystemIcons = Join-Path $Root 'assets\icons\system'

New-Item -ItemType Directory -Force -Path `
    (Join-Path $FigmaRoot 'menu-bar'), `
    (Join-Path $FigmaRoot 'dock'), `
    (Join-Path $FigmaRoot 'icons'), `
    (Join-Path $FigmaRoot 'wallpapers'), `
    $SystemIcons | Out-Null

$stage = Join-Path $env:TEMP ("bndz-figma-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $stage | Out-Null

try {
    if ($SourceZip) {
        if (-not (Test-Path $SourceZip)) { throw "Zip not found: $SourceZip" }
        Expand-Archive -Path $SourceZip -DestinationPath $stage -Force
    }
    elseif ($SourceDir) {
        if (-not (Test-Path $SourceDir)) { throw "Folder not found: $SourceDir" }
        Copy-Item -Path (Join-Path $SourceDir '*') -Destination $stage -Recurse -Force
    }
    else {
        throw "Provide -SourceZip or -SourceDir"
    }

    $nameMap = @{
        'apple-mark.png'       = 'menu-bar\apple-mark.png'
        'apple.png'            = 'menu-bar\apple-mark.png'
        'control-center.png'   = 'menu-bar\control-center.png'
        'spotlight.png'        = 'menu-bar\spotlight.png'
        'search.png'           = 'menu-bar\spotlight.png'
        'dock-glass.png'       = 'dock\dock-glass.png'
        'dock.png'             = 'dock\dock-glass.png'
        'separator.png'        = 'dock\separator.png'
        'finder.png'           = 'icons\finder.png'
        'launchpad.png'        = 'icons\launchpad.png'
        'safari.png'           = 'icons\safari.png'
        'messages.png'         = 'icons\messages.png'
        'mail.png'             = 'icons\mail.png'
        'maps.png'             = 'icons\maps.png'
        'photos.png'           = 'icons\photos.png'
        'facetime.png'         = 'icons\facetime.png'
        'calendar.png'         = 'icons\calendar.png'
        'contacts.png'         = 'icons\contacts.png'
        'notes.png'            = 'icons\notes.png'
        'apple-tv.png'         = 'icons\apple-tv.png'
        'tv.png'               = 'icons\apple-tv.png'
        'music.png'            = 'icons\music.png'
        'podcasts.png'         = 'icons\podcasts.png'
        'reminders.png'        = 'icons\reminders.png'
        'stocks.png'           = 'icons\stocks.png'
        'app-store.png'        = 'icons\app-store.png'
        'settings.png'         = 'icons\settings.png'
        'preferences.png'      = 'icons\settings.png'
        'system-settings.png'  = 'icons\settings.png'
        'news.png'             = 'icons\news.png'
        'voice-memos.png'      = 'icons\voice-memos.png'
        'downloads.png'        = 'icons\downloads.png'
        'folder.png'           = 'icons\downloads.png'
        'trash.png'            = 'icons\trash.png'
        'weather.png'          = 'icons\weather.png'
    }

    $wallpaperNames = @('sequoia-day.jpg','sequoia-night.jpg','tahoe-dark.jpg','wallpaper-sequoia.jpg','wallpaper-sonoma.jpg','wallpaper-tahoe-night.jpg')

    $copied = 0
    Get-ChildItem -Path $stage -Recurse -File | ForEach-Object {
        $name = $_.Name.ToLowerInvariant()
        if ($nameMap.ContainsKey($name)) {
            $dest = Join-Path $FigmaRoot $nameMap[$name]
            Copy-Item $_.FullName $dest -Force
            Write-Host "  + $($nameMap[$name])" -ForegroundColor Green
            $copied++
        }
        elseif ($wallpaperNames -contains $name -or $name -match '\.(jpg|jpeg|png)$' -and $name -match 'wall|tahoe|sequoia|sonoma') {
            $dest = Join-Path $FigmaRoot "wallpapers\$name"
            Copy-Item $_.FullName $dest -Force
            Write-Host "  + wallpapers\$name" -ForegroundColor Green
            $copied++
        }
        elseif ($name -eq 'tokens.json') {
            Copy-Item $_.FullName (Join-Path $FigmaRoot 'tokens.json') -Force
            Write-Host "  + tokens.json" -ForegroundColor Cyan
            $copied++
        }
    }

    # Sync Figma icons into icons/system for existing pipeline IDs
    $systemSync = @{
        'finder.png' = 'finder.png'
        'launchpad.png' = 'launchpad.png'
        'calendar.png' = 'calendar.png'
        'trash.png' = 'trash.png'
        'weather.png' = 'weather.png'
        'settings.png' = 'preferences.png'
    }
    foreach ($pair in $systemSync.GetEnumerator()) {
        $src = Join-Path $FigmaRoot "icons\$($pair.Key)"
        if (Test-Path $src) {
            Copy-Item $src (Join-Path $SystemIcons $pair.Value) -Force
            Write-Host "  sync system/$($pair.Value)" -ForegroundColor Yellow
        }
    }

    if ($copied -eq 0) {
        Write-Warning "No recognized filenames found. See BndzFinder/assets/figma/README.md for expected names."
    }
    else {
        Write-Host "`nImported $copied asset(s) into $FigmaRoot" -ForegroundColor Cyan
        Write-Host "Rebuild with .\\run.cmd" -ForegroundColor Cyan
    }
}
finally {
    if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
}
