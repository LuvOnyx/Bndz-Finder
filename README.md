# Bndz-Finder

Premium macOS-style Windows shell overlay — MyDock, MyFinder, Launchpad, and Stage Manager.

## Stack

- **C# 13 / .NET 9**
- **WinUI 3** (Windows App SDK 1.6+) for all UI surfaces
- **D3D11** (Vortice) for dock glass and minimize animations
- **CsWin32** for shell integration (AppBar, hooks, tray mirror)

## Requirements

- Windows 11 x64 (primary), Windows 10 1809+ (best-effort)
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Windows App SDK 1.6+ (installed with WinUI projects)

## Build

### Cross-platform (libraries + tests — Linux/macOS/CI)

```bash
bash BndzFinder/scripts/build.sh
# or
bash BndzFinder/scripts/dev.sh
```

### Full Windows build (WinUI 3 + dock UI)

**There is no `dist/` folder** — output is under `bin/` or `bin/Publish/Portable/`.

Pick the command that matches your folder layout (see [WINDOWS_DEV.md](BndzFinder/docs/WINDOWS_DEV.md)):

```powershell
# Layout A: repo root (README.md + BndzFinder/ subfolder)
pwsh -File .\build.ps1
pwsh -File .\build.ps1 -Publish
pwsh -File .\run.ps1

# Layout B: inner BndzFinder folder (BndzFinder.sln is here)
pwsh -File .\scripts\build.ps1
pwsh -File .\scripts\build.ps1 -Publish
```

### Run on Windows

```powershell
# One command (from repo root)
pwsh -File .\run.ps1

# Or two terminals (from folder with BndzFinder.sln)
dotnet run --project src\BndzFinder.ShellHost\BndzFinder.ShellHost.csproj -c Release
dotnet run --project src\BndzFinder.App\BndzFinder.App.csproj -c Release
```

## Premium Dock Features (this release)

- **Glass backdrops**: Translucent, Acrylic, Mica, Liquid Glass with configurable blur/opacity/saturation
- **Icon zoom**: macOS fisheye magnification with Scale, Select, Light, Scale+Light hover effects
- **3D immersion**: tilt + vertical lift on magnified icons
- **Icon reflections**: configurable opacity/blur with shadow layers
- **Running indicators**: per-app dots on dock icons
- **WinUI dock bar**: `DockBarControl` with `AcrylicBrush`, `DockIconControl` with live effect binding

## Project Layout

See [docs/ARCHITECTURE.md](BndzFinder/docs/ARCHITECTURE.md) for process model and IPC contracts.

## Parity Tracking

Feature parity with MyDockFinder is tracked in [docs/PARITY_CHECKLIST.md](BndzFinder/docs/PARITY_CHECKLIST.md).

## Settings

Stored at `%APPDATA%\BndzFinder\settings.json`.

Icon cache at `%APPDATA%\BndzFinder\IconCache\`.

Themes at `%APPDATA%\BndzFinder\themes\`.
