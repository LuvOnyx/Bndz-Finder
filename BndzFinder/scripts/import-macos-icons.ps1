#Requires -Version 7.0
<#
.SYNOPSIS
    Download official macOS-style system dock icons from macosicons.com API.
.DESCRIPTION
    Replaces hand-made SVG placeholders with PNG icons from macosicons.com.
    Requires a free API key: https://docs.macosicons.com/api-management
.PARAMETER ApiKey
    macosicons.com API key. Falls back to MACOSICONS_API_KEY environment variable.
.PARAMETER ManifestPath
    Path to assets/icons/system/manifest.json
#>
param(
    [string]$ApiKey = $env:MACOSICONS_API_KEY,
    [string]$ManifestPath = (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '..\assets\icons\system\manifest.json')
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($ApiKey)) {
    Write-Error @"
MACOSICONS_API_KEY is required.

1. Sign in at https://docs.macosicons.com/api-management
2. Create a free API key (50 requests/month)
3. Run:
   `$env:MACOSICONS_API_KEY = 'your-key'
   pwsh -File BndzFinder/scripts/import-macos-icons.ps1
"@
}

if (-not (Test-Path $ManifestPath)) {
    Write-Error "Missing manifest: $ManifestPath"
}

$manifest = Get-Content $ManifestPath -Raw | ConvertFrom-Json
$outDir = Split-Path -Parent $ManifestPath
$reportPath = Join-Path $outDir 'import-report.json'
$results = @()

function Search-MacosIcon {
    param([string]$Query)

    $body = @{
        query = $Query
        searchOptions = @{
            hitsPerPage = 20
            sort = @('downloads:desc')
            page = 1
        }
    } | ConvertTo-Json -Depth 5

    $response = Invoke-RestMethod `
        -Uri 'https://api.macosicons.com/api/search' `
        -Method Post `
        -Headers @{ 'x-api-key' = $ApiKey; 'Content-Type' = 'application/json' } `
        -Body $body

    return $response
}

function Select-BestHit {
    param($Hits, [string]$PreferAppName)

    if ($null -eq $Hits -or $Hits.Count -eq 0) { return $null }

    $exact = $Hits | Where-Object { $_.appName -eq $PreferAppName } | Select-Object -First 1
    if ($exact) { return $exact }

    $contains = $Hits | Where-Object { $_.appName -like "*$PreferAppName*" } | Select-Object -First 1
    if ($contains) { return $contains }

    return $Hits | Select-Object -First 1
}

Write-Host "Importing $($manifest.icons.Count) system icons from macosicons.com..." -ForegroundColor Cyan

foreach ($icon in $manifest.icons) {
    Write-Host "  $($icon.id) <- query '$($icon.query)'" -ForegroundColor Gray
    $search = Search-MacosIcon -Query $icon.query
    $hit = Select-BestHit -Hits $search.hits -PreferAppName $icon.preferAppName

    if ($null -eq $hit) {
        Write-Warning "No hit for $($icon.id) (query: $($icon.query))"
        $results += [ordered]@{
            id = $icon.id
            status = 'missing'
            query = $icon.query
        }
        continue
    }

    $dest = Join-Path $outDir $icon.file
    Invoke-WebRequest -Uri $hit.lowResPngUrl -OutFile $dest
    Write-Host "    -> $($hit.appName) by $($hit.uploadedBy) ($dest)" -ForegroundColor Green

    $results += [ordered]@{
        id = $icon.id
        status = 'ok'
        file = $icon.file
        appName = $hit.appName
        credit = $hit.uploadedBy
        sourceUrl = $hit.lowResPngUrl
        downloads = $hit.downloads
    }
}

$report = [ordered]@{
    importedAt = (Get-Date).ToString('o')
    source = $manifest.source
    results = $results
}
$report | ConvertTo-Json -Depth 6 | Set-Content $reportPath -Encoding UTF8

$missing = @($results | Where-Object { $_.status -eq 'missing' })
if ($missing.Count -gt 0) {
    Write-Warning "$($missing.Count) icon(s) missing. Open macosicons.com and download manually into $outDir"
    exit 1
}

Write-Host "Done. Import report: $reportPath" -ForegroundColor Cyan
