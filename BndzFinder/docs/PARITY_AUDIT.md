# Bndz-Finder Parity Audit

**Audit date:** 2026-07-12  
**Reference:** [`PARITY_CHECKLIST.md`](PARITY_CHECKLIST.md) (296 items)  
**Standard:** Premium-only — field-backed MVVM, `PremiumDockLayoutEngine`, `PremiumColorPicker`, IPC-first ShellHost wiring.

This document is an honest engineering audit. Items are **not** marked verified until manually confirmed on Windows 11 x64. Linux CI builds libraries + 31 tests only; WinUI projects require a Windows `.\run.cmd` gate.

---

## Executive summary

| Status | Count (approx.) | Meaning |
|--------|-----------------|--------|
| **Implemented** | ~45 | Code exists, wired to settings/IPC, expected to work on Windows |
| **Partial** | ~90 | Scaffold or UI exists; behavior incomplete or unverified |
| **Scaffold** | ~120 | Types/settings/tests only; no user-visible behavior |
| **Missing** | ~41 | No meaningful implementation |

**Verified on Windows:** 0 / 296 (QA not run in this environment)

---

## Phase 3 wiring completed in this audit

| Area | Change |
|------|--------|
| **IPC** | App subscribes to `TrayIconsUpdated`, `WindowListUpdated`; `ShellIpcParsers` for JSON payloads |
| **Hotkeys / corners** | `HandleHotkey` maps `corner:*`, dock/finder/launchpad/stage aliases; `HotCornerMonitor` reads `HotCorners` settings |
| **Settings sync** | `SettingsReload` IPC reapplies hotkeys + hot corners in ShellHost after App save |
| **Tray** | ShellHost broadcasts `OwnerWindow`; Finder uses IPC tray (no local poll overwrite) |
| **Dock** | System items route correctly; folder-stack flyout; hover preview flyout with capture |
| **Stage Manager** | IPC window list + `PrintWindow` thumbnails rendered in strip; focus/close actions |
| **Launchpad** | Escape dismiss, pagination UI, overlay controller hide on launch |
| **Preferences** | Look & Behavior, Audio/Display/Network sections; dock/finder hotkeys; hot-corner pickers |

---

## MyDock

| Category | Status | Notes |
|----------|--------|-------|
| Position / layout | **Partial** | Settings + AppBar registration; edge activation timer in `DockWindow` |
| Display modes | **Partial** | `DockBehaviorService.ShouldShowDock` implements logic; hide-delay timer not fully wired |
| Icons / fisheye | **Implemented** | `PremiumDockLayoutEngine`, magnification, hover effects |
| Folder stacks | **Partial** | `FolderStackFlyout` + `FolderStackViewModel`; fan/grid sort in service, basic flyout UI |
| System dock items | **Implemented** | Finder, Launchpad, Prefs, Trash, Calendar, Weather routes fixed |
| Window preview | **Partial** | Delayed flyout with `PrintWindow` capture; not DWM thumbnail API |
| Minimize animations | **Scaffold** | `MinimizeAnimationEngine` + ShellHost hook; render paths no-op |
| Taskbar hide | **Partial** | `ITaskbarController` called at startup; not continuously synced |
| Badges / progress | **Scaffold** | `BadgePollingService`, `ProgressBarMirrorService` — polling only |
| D3D11 compositor | **Scaffold** | Vortice reference; no visible compositor output |

---

## MyFinder

| Category | Status | Notes |
|----------|--------|-------|
| Bar chrome | **Partial** | Acrylic bar, height setting, widget visibility toggles |
| Metrics widgets | **Partial** | WMI polling for CPU/RAM/battery; GPU/disk/network stubbed to 0 |
| Tray proxy | **Partial** | IPC sync + click forward; tooltip text buttons (no icon bitmap yet) |
| Control Center | **Partial** | Flyout shells for audio/bluetooth/display |
| Stage Manager in Finder | **Missing** | `ShowStageManagerInFinder` not embedded in bar |
| AppBar top reservation | **Missing** | Finder does not register top AppBar |

---

## Launchpad

| Category | Status | Notes |
|----------|--------|-------|
| Invocation | **Implemented** | Hotkey, dock item, hot corner → overlay |
| Dismiss | **Implemented** | Escape, launch, `HideLaunchpad` |
| Catalog | **Partial** | Start menu / desktop `.lnk` scan; FontIcon placeholders |
| Search | **Implemented** | Case-insensitive filter, clears on close |
| Pagination | **Partial** | Prev/next + page indicator; not swipe animation |
| HD icons | **Missing** | `LaunchpadHdIcons` setting unused in UI |

---

## Stage Manager

| Category | Status | Notes |
|----------|--------|-------|
| Window enumeration | **Implemented** | ShellHost IPC + local poll fallback |
| Thumbnails | **Partial** | `PrintWindow` BGRA → `WriteableBitmap`; not DWM live thumb |
| Actions | **Implemented** | Click focus, right-tap close |
| Desktop icons / media | **Missing** | `DesktopIconMode`, media pause not wired |
| Finder embed | **Missing** | Strip is separate overlay window only |

---

## Hotkeys

| Category | Status | Notes |
|----------|--------|-------|
| Registration | **Implemented** | `HotkeySyncService` + `GlobalHotkeyService` in ShellHost |
| IPC delivery | **Implemented** | `HotkeyPressed` → `ShellOverlayController` |
| Preferences UI | **Partial** | Read-only display for dock/finder/launchpad/stage; no editor |
| Reload on save | **Implemented** | `SettingsReload` IPC |

---

## Hot Corners

| Category | Status | Notes |
|----------|--------|-------|
| Detection | **Implemented** | Bottom-left/right, 8px zone, settings-driven |
| Actions | **Partial** | Launchpad, Stage Manager, Show Desktop, Start, Action Center, Lock, Display Sleep, Widgets |
| Preferences | **Partial** | Combo boxes for left/right corners |
| Trigger delay | **Missing** | No debounce delay setting |

---

## Preferences

| Section | Status |
|---------|--------|
| General | **Implemented** |
| Appearance | **Implemented** (premium color pickers) |
| System Icon Tray | **Implemented** |
| Screen | **Implemented** |
| Look and Behavior | **Implemented** (this audit) |
| Launchpad | **Partial** (size/labels/source) |
| Window Animations | **Partial** (effect picker only) |
| Audio / Display / Network | **Implemented** (this audit) |
| Hot Corners and Hotkeys | **Partial** (display + corner pickers) |
| Themes | **Partial** (backup button) |

---

## Theming / Startup / Notifications

| Area | Status |
|------|--------|
| Theme packs | **Scaffold** — import/list services exist, limited UI |
| Startup modes | **Scaffold** — `StartupService` types, not wired to installer |
| Notifications | **Scaffold** — toggle exists, no Action Center open on click |

---

## Build & quality gates

| Gate | Status |
|------|--------|
| Linux `build.sh` | **Pass** — libraries + 31 tests |
| Windows full solution | **Requires user** — `git pull` then `.\run.cmd` |
| MVVM pattern | **Field-backed** `[ObservableProperty] private T _field` — no partial properties on WinUI |
| PRI generation | **App only** — class libraries disable PRI via `Directory.Build.Windows.props` |

---

## Recommended Windows verification (first session)

1. `git pull` on branch `cursor/bndz-finder-foundation-152a`
2. `.\run.cmd` — report first 10 errors if any
3. Confirm ShellHost starts (tray icons appear in Finder after ~1s)
4. Win+L opens Launchpad; Escape closes
5. Dock Preferences icon opens prefs (not Finder)
6. Stage Manager strip shows window titles + thumbnail images
7. Save hot corner change in prefs → corner action updates without restart

---

## Honest product statement

Bndz-Finder remains a **premium foundation**, not MyDockFinder parity. This audit closes major wiring gaps from the scaffold phase. Remaining work for production parity includes DWM thumbnails, visible minimize overlay, drag-reorder dock, full hotkey editor, notification center integration, theme pack application, and systematic Windows QA against all 296 checklist items.
