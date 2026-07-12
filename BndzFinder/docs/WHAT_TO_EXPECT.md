# What to expect when the build succeeds

**Be honest:** This is a **foundation / scaffold**, not a finished MyDockFinder replacement.

## Parity status

See `docs/PARITY_CHECKLIST.md` — **0 of 296** parity items are manually verified on Windows yet. Most settings exist in code; many UI flows are partial.

## What should work after `.\run.cmd`

| Component | Expect |
|-----------|--------|
| **ShellHost** | Starts in background — hooks, hotkeys, tray mirror, IPC |
| **App** | WinUI window with dock bar, finder strip, overlays |
| **Dock** | Glass bar, default icons, basic magnification layout |
| **Finder** | Menu bar with optional widgets (off by default in settings) |
| **Launchpad / Stage Manager** | UI shells — search grid, window strip |
| **Preferences** | Settings panels — save to `%APPDATA%\BndzFinder\settings.json` |

## What is NOT there yet (or is stubbed)

- Full macOS-level polish and animations everywhere
- DWM live thumbnails in Stage Manager
- Window preview flyout on dock hover (scaffold only)
- Visible minimize-to-dock D3D overlay (engine scaffold)
- Complete taskbar replacement / full shell takeover
- 296-point MyDockFinder parity QA

## If build fails

```powershell
git pull
.\run.cmd
```

Paste the **first 10 `error` lines** only — not the full log.
