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

```powershell
cd BndzFinder
dotnet build BndzFinder.sln -c Release
```

Run the host (Windows):

```powershell
dotnet run --project src/BndzFinder.App/BndzFinder.App.csproj
```

Run ShellHost companion (required for minimize-to-dock):

```powershell
dotnet run --project src/BndzFinder.ShellHost/BndzFinder.ShellHost.csproj
```

## Project Layout

See [docs/ARCHITECTURE.md](BndzFinder/docs/ARCHITECTURE.md) for process model and IPC contracts.

## Parity Tracking

Feature parity with MyDockFinder is tracked in [docs/PARITY_CHECKLIST.md](BndzFinder/docs/PARITY_CHECKLIST.md).

## Settings

Stored at `%APPDATA%\BndzFinder\settings.json`.

Icon cache at `%APPDATA%\BndzFinder\IconCache\`.

Themes at `%APPDATA%\BndzFinder\themes\`.
