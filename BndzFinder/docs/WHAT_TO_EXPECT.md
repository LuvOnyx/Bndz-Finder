# What to expect when the build succeeds

**Be honest:** Bndz-Finder targets **MyDockFinder parity** (see `docs/BUILD_PLAN.md`). Phase 4 visual completion is in progress — running apps, Finder bar, and real icons ship in the current sprint; minimize animations and full 296-item QA remain.

## Parity status

See `docs/PARITY_CHECKLIST.md` — **0 of 296** parity items are manually verified on Windows yet. See `docs/PARITY_AUDIT.md` for an honest per-area engineering audit (implemented / partial / scaffold).

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
