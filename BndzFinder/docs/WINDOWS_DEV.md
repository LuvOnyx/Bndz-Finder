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
| `NU1301` / `No such host is known` (nuget.org) | **Network/DNS issue** — not a code bug. Verify `ping www.nuget.org`, disable VPN/proxy, check firewall. First WinUI restore needs internet for `Microsoft.WindowsAppSDK`. Retry: `dotnet nuget locals all --clear` then `.\build.cmd` |
| SDK 10 with pinned .NET 9 | Install [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) alongside SDK 10, or use `global.json` rollForward |
| WinUI build fails | Install Windows App SDK / VS Build Tools with C++ workload |
| No dock visible | Ensure ShellHost is running; check single-instance lock in `%TEMP%` |
