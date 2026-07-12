# Running Bndz-Finder on Windows

## There is no `dist/` folder

This is a **.NET 10 WinUI 3** desktop app. Build output goes to:

| Mode | Output path |
|------|-------------|
| Build only | `src\BndzFinder.App\bin\Release\net10.0-windows10.0.22621.0\win-x64\BndzFinder.App.exe` |
| `-Publish` | `src\BndzFinder.App\bin\Publish\Portable\win-x64\BndzFinder.App.exe` |
| ShellHost publish | `src\BndzFinder.ShellHost\bin\Publish\Portable\win-x64\BndzFinder.ShellHost.exe` |

## Prerequisites

1. **Windows 11 x64** (recommended)
2. **[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)** — verify with `dotnet --version` (should show `10.0.x`)
3. **PowerShell 7+** (`pwsh`) or Windows PowerShell 5.1

## Which folder am I in?

After cloning, you may have **one of two layouts**:

### Layout A — Full repo (GitHub default)

```
Bndz-Finder\          ← repo root (has README.md)
  BndzFinder\         ← solution folder (has BndzFinder.sln)
    scripts\build.ps1
    src\...
  build.ps1           ← wrapper at repo root
  run.ps1
```

From repo root:
```powershell
cd C:\Users\you\Desktop\Bndz-Finder
pwsh -File .\build.ps1
pwsh -File .\build.ps1 -Publish
pwsh -File .\run.ps1
```

### Layout B — Inner folder only (your case if `BndzFinder\scripts` fails)

```
BndzFinder\           ← you are HERE (has BndzFinder.sln directly)
  scripts\build.ps1
  src\...
```

From inner folder:
```powershell
cd C:\Users\mikey\Desktop\BndzFinder
pwsh -File .\scripts\build.ps1
pwsh -File .\scripts\build.ps1 -Publish
```

**Do not** use `BndzFinder\scripts\build.ps1` unless you are in the parent repo root (Layout A).

## Quick diagnose

```powershell
# Where am I?
Get-Location

# Does the solution exist here?
Test-Path .\BndzFinder.sln          # Layout B — should be True
Test-Path .\BndzFinder\BndzFinder.sln  # Layout A — should be True

# Does the build script exist?
Test-Path .\scripts\build.ps1
Test-Path .\BndzFinder\scripts\build.ps1
```

Use whichever path returns `True`.

## Build commands (use `.cmd` — works with default Windows security policy)

```powershell
cd C:\Users\you\Desktop\Bndz-Finder-cursor-bndz-finder-foundation-152a

# Recommended — no execution policy issues
.\run.cmd
.\build.cmd
.\publish.cmd

# Or explicitly bypass policy for .ps1
pwsh -ExecutionPolicy Bypass -File .\run.ps1
pwsh -ExecutionPolicy Bypass -File .\build.ps1 -Publish
```

### Inner BndzFinder folder only

```powershell
.\BndzFinder\run.cmd
.\BndzFinder\scripts\build.cmd
.\BndzFinder\scripts\publish.cmd
```

## Run (development)

Bndz-Finder uses **two processes**:

1. **ShellHost** — global hooks, hotkeys, tray mirror, minimize interception
2. **App** — WinUI dock, finder, launchpad, stage manager

**Option 1 — one command** (from repo root with `run.ps1`):
```powershell
pwsh -File .\run.ps1
```

**Option 2 — two terminals** (from folder containing `BndzFinder.sln`):
```powershell
# Terminal 1
dotnet run --project src\BndzFinder.ShellHost\BndzFinder.ShellHost.csproj -c Release

# Terminal 2 — run the built exe (NOT dotnet run — WinUI native DLLs fail with 0xC0000135)
src\BndzFinder.App\bin\Release\net10.0-windows10.0.22621.0\win-x64\BndzFinder.App.exe
```

## Run (portable publish)

```powershell
pwsh -File .\scripts\build.ps1 -Publish

$host = "src\BndzFinder.ShellHost\bin\Publish\Portable\win-x64\BndzFinder.ShellHost.exe"
$app  = "src\BndzFinder.App\bin\Publish\Portable\win-x64\BndzFinder.App.exe"
Start-Process $host
Start-Sleep -Seconds 2
Start-Process $app
```

## First restore timing

The **first** `dotnet restore` on a fresh clone is slow — that is normal.

| Phase | Typical time (good WiFi) |
|-------|--------------------------|
| First restore (WinUI / Windows App SDK) | **5–15 minutes** |
| Later restores (packages cached) | **10–30 seconds** |
| Full Release build (after restore) | **2–5 minutes** |

WinUI pulls large packages (`Microsoft.WindowsAppSDK`, `Microsoft.Windows.SDK.BuildTools`, CommunityToolkit WinUI, etc.). Brief WiFi drops are handled automatically — see below.

### Flaky WiFi / provider outages

`build.ps1` and `run.cmd`:

1. **Wait** for NuGet to come back if you start offline (up to 30 minutes)
2. **Auto-retry** restore up to **15 times** on transient errors (`NU1301`, DNS blips, timeouts)
3. **Fail fast** on compile/MSBuild errors — build and test do not retry
4. **Resume** from `%USERPROFILE%\.nuget\packages` — packages already downloaded are **not** re-fetched

You do **not** need to restart manually after a split-second dropout. Leave `.\run.cmd` running; it will pause and retry.

```powershell
.\run.cmd
# Validate MSBuild wiring first (optional)
pwsh -ExecutionPolicy Bypass -File .\BndzFinder\scripts\preflight.ps1
# Optional: more retries on very bad connections
pwsh -ExecutionPolicy Bypass -File .\BndzFinder\scripts\build.ps1 -NetworkRetries 25
```

Only clear the cache if restore fails repeatedly with **corrupt package** errors:

```powershell
dotnet nuget locals all --clear
.\build.cmd
```

## Settings

- `%APPDATA%\BndzFinder\settings.json`
- Widgets and overlays are **optional** — configure in Preferences (system dock item or hotkey)

## Common errors

| Error | Fix |
|-------|-----|
| `script file is not recognized` | Wrong path — use `.\scripts\build.ps1` not `BndzFinder\scripts\...` when already inside inner folder |
| `not digitally signed` / execution policy | Use **`.\run.cmd`** or **`.\build.cmd`** instead of `.ps1`, or `pwsh -ExecutionPolicy Bypass -File .\run.ps1` |
| `BndzFinder.sln not found` | `cd` to folder containing `BndzFinder.sln` |
| `dotnet not found` | Install .NET 10 SDK |
| `NU1101` Unable to find package | Bad package ID or wrong NuGet feed — **not WiFi**. This repo uses `nuget.org` only via `BndzFinder/nuget.config` |
| `NU1301` / `No such host is known` (nuget.org) | **Network dropped or DNS issue.** Build auto-retries; reconnect WiFi and leave `.\run.cmd` running |
| `MSB3073` XamlCompiler exited with code 1 | Invalid XAML — e.g. `UniformGrid` (not in WinUI 3), wrong `AcrylicBrush` placement. Fixed in latest branch |
| `MSB4062` ExpandPriContent / Pri.Tasks.dll | Set `EnableMsixTooling=true` in `Directory.Build.Windows.props` (dotnet-build-compatible PRI tasks). Class libraries also import `WinUiClassLibrary.props` with `MrtCoreEnablePriGeneration=false`. Only `BndzFinder.App` (WinExe) generates PRI. |
| `CS9035` Required member not set | Remove `required` from types WinUI XAML activates (`DockIconViewModel`, etc.) — XAML codegen uses parameterless construction. |
| `MVVMTK0045` | WinUI ViewModels use field-backed `[ObservableProperty]` (partial-property pattern fails on SDK 10 WinUI builds with CS9248). Warning suppressed in `Directory.Build.Windows.props`. |
| `NU1504` duplicate packages | Central versions in `Directory.Packages.props`; `CommunityToolkit.Mvvm` only in `Directory.Build.Windows.props`. |
| Build retries on compile errors | `build.ps1` retries **restore only**. `CS9035` used to false-match `503` — fixed. Run `scripts\preflight.ps1` before building. |
| WinUI build fails | Install Windows App SDK / VS Build Tools with C++ workload |
| No dock visible | Ensure ShellHost is running; check single-instance lock in `%TEMP%` |
| App exits `-1073741189` (0xC0000135) | **Do not** `dotnet run` the App. Use `.\run.cmd` or run `BndzFinder.App.exe` from `bin\Release\...\win-x64\`. Delete `bin`/`obj` and rebuild if DLLs are missing. |
| `0x80670016` Package dependency could not be resolved | Self-contained bootstrap could not find WinUI runtime. Ensure `Microsoft.ui.xaml.dll` is beside `BndzFinder.App.exe`. Delete `src\BndzFinder.App\bin` and `obj`, run `.\run.cmd`. Or install [Windows App SDK 1.6 runtime](https://aka.ms/windowsappsdk/1.6/latest/windowsappruntimeinstall-x64.exe). |
