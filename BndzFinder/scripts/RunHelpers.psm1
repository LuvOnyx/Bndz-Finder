#Requires -Version 5.1

function Get-BndzAppExe {
    param(
        [string]$ProjectRoot,
        [string]$Configuration
    )

    $binRoot = Join-Path $ProjectRoot "src\BndzFinder.App\bin\$Configuration"
    if (-not (Test-Path $binRoot)) { return $null }

    $candidates = Get-ChildItem -Path $binRoot -Recurse -Filter 'BndzFinder.App.exe' -ErrorAction SilentlyContinue |
        Where-Object { $_.DirectoryName -match 'win-x64' } |
        Sort-Object LastWriteTime -Descending

    return $candidates | Select-Object -First 1
}

function Test-BndzWinUiRuntime {
    param([string]$ExePath)

    $dir = Split-Path -Parent $ExePath
    $required = @(
        'Microsoft.ui.xaml.dll',
        'Microsoft.WindowsAppRuntime.dll'
    )

    $missing = @()
    foreach ($dll in $required) {
        if (-not (Test-Path (Join-Path $dir $dll))) {
            $missing += $dll
        }
    }

    return $missing
}

function Format-BndzExitCodeHint {
    param([int]$ExitCode)

    if ($ExitCode -eq -1073741189) {
        return @(
            'Exit code 0xC0000135 = a native DLL failed to load (WinUI / Windows App SDK).',
            'Do NOT use ''dotnet run'' for the App — run.ps1 now launches the built .exe.',
            'If this persists: delete src\BndzFinder.App\bin and obj, then rebuild with .\run.cmd'
        ) -join "`n"
    }

    return 'Common causes: another instance running, missing Windows App SDK runtime, or a startup exception.'
}

Export-ModuleMember -Function Get-BndzAppExe, Test-BndzWinUiRuntime, Format-BndzExitCodeHint
