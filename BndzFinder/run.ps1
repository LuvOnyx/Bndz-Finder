#Requires -Version 5.1
# Run from inner BndzFinder folder (where BndzFinder.sln lives)
$InnerRoot = $PSScriptRoot
$RepoRoot = Split-Path -Parent $InnerRoot
if (Test-Path (Join-Path $RepoRoot 'run.ps1')) {
    & (Join-Path $RepoRoot 'run.ps1') @PSBoundParameters
} else {
    & (Join-Path $InnerRoot 'scripts\run-inner.ps1') @PSBoundParameters
}
