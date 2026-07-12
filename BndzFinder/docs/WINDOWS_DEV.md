# Running Bndz-Finder on Windows

## There is no `dist/` folder

This is a **.NET 9 WinUI 3** desktop app. Build output goes to:

| Mode | Output path |
|------|-------------|
| Build only | `src\BndzFinder.App\bin\Release\net9.0-windows10.0.22621.0\win-x64\BndzFinder.App.exe` |
| `-Publish` | `src\BndzFinder.App\bin\Publish\Portable\win-x64\BndzFinder.App.exe` |
| ShellHost publish | `src\BndzFinder.ShellHost\bin\Publish\Portable\win-x64\BndzFinder.ShellHost.exe` |

## Prerequisites

1. **Windows 11 x64** (recommended)
2. **[.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)** — verify with `dotnet --version`
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

# Terminal 2
dotnet run --project src\BndzFinder.App\BndzFinder.App.csproj -c Release
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

`build.ps1` and `run.cmd` now:

1. **Wait** for NuGet to come back if you start offline (up to 30 minutes)
2. **Auto-retry** restore up to **15 times** on transient errors (`NU1301`, DNS blips, timeouts)
3. **Resume** from `%USERPROFILE%\.nuget\packages` — packages already downloaded are **not** re-fetched

You do **not** need to restart manually after a split-second dropout. Leave `.\run.cmd` running; it will pause and retry.

```powershell
.\run.cmd
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
| `dotnet not found` | Install .NET 9 SDK |
| `NU1101` Unable to find package | Bad package ID or wrong NuGet feed — **not WiFi**. This repo uses `nuget.org` only via `BndzFinder/nuget.config` |
| `NU1301` / `No such host is known` (nuget.org) | **Network dropped or DNS issue.** Build auto-retries; reconnect WiFi and leave `.\run.cmd` running |
| SDK 10 active (`sdk\10.0.201` in errors) | **Install .NET 9 SDK** — WinUI breaks on SDK 10 (MSB4062 Pri tasks, XamlCompiler). Verify: `dotnet --list-sdks` shows `9.0.x`, then `dotnet --version` in `BndzFinder\` shows 9.x |
| `MVVMTK0007` / `SetWidgetEnabledCommand` missing | Fixed in latest branch — `git pull` and rebuild |
| WinUI build fails | Install Windows App SDK / VS Build Tools with C++ workload |
| No dock visible | Ensure ShellHost is running; check single-instance lock in `%TEMP%` |
