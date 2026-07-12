# Bndz-Finder Build Plan — MyDockFinder Parity

**Product target:** [MyDockFinder](https://www.mydockfinder.com/index_en.html) — improved replacement with WinUI 3 blur/Mica, premium glass dock, modern C# architecture.

**Contract:** [`PARITY_CHECKLIST.md`](PARITY_CHECKLIST.md) — 296 user-visible behaviors.

---

## Reconstructed phases (from repo evidence)

| Phase | Scope | Test anchor | Status |
|-------|--------|-------------|--------|
| **1 — Premium foundation** | Settings schema, fisheye layout, glass/theming, icon pipeline | `PremiumDockTests` | ✅ Libraries complete |
| **2 — Shell primitives** | Folder stacks, hotkeys, AppBar (dock) | `Phase2Phase3Tests` | ✅ Partial |
| **3 — IPC wiring** | ShellHost pipe, tray/window broadcast, overlay routing, prefs | `PARITY_AUDIT.md` Phase 3 | ✅ Wired, unverified on Windows |
| **4 — Visual completion** | Running apps, real icons, Finder bar, hide-delay, drag-reorder, minimize overlay, DWM | *No Phase4 tests — never executed* | 🚧 **In progress** |
| **5 — Polish services** | Weather, progress mirror, hotkey sync | `Phase5FeatureTests` | ⚠️ Scaffold |
| **Completion plan** | Ship acceptance criteria | `CompletionPlanTests` | ⚠️ Logic only, not UX |

---

## Phase 4 — what was NEVER done (blockers)

These are why the product does not look or behave like MyDockFinder:

1. **Dock shows 6 system shortcuts only** — no running/pinned application sync
2. **App icons not extracted** — `.exe` paths draw a blue placeholder; system PNGs require macosicons import
3. **Finder bar hidden on startup** — no top AppBar reservation
4. **Tray mirror is text-only** — IPC omits icon bitmaps
5. **Launchpad uses FontIcon placeholders** — ignores catalog icon paths
6. **Hide-delay setting unused** — `HideDockDelayMs` never enforced
7. **Minimize animations invisible** — render paths are no-op
8. **296-item Windows QA** — 0 verified

---

## Execution order (current sprint)

### Sprint A — Runnable product shell *(this branch)*

- [x] Fix WinUI 3 `OnExit` build error
- [x] **Taskbar replacement mode** — hides native taskbar + watchdog while shell runs
- [x] `RunningAppSyncService` — enumerate open windows → dock items
- [x] `WindowsFileIconExtractor` — SHGetFileInfo → PNG cache for exe/lnk
- [x] **macOS squircle system icon fallbacks** (1024px Apple proportions)
- [x] Finder visible on launch + top `AppBarService` registration
- [x] **Dock context menus** (Open, Keep in Dock, Quit, Remove, Preferences)
- [x] Tray `IconData` over IPC + image buttons in Finder
- [x] Launchpad `Image` binding from icon pipeline
- [x] Dock hide-delay timer

### Sprint B — MyDockFinder signature features

- [ ] Window preview via DWM (`WindowPreviewService`)
- [ ] Visible minimize-to-dock overlay (Genie/Scale/Suck/ScaleDX)
- [ ] Drag-and-drop reorder + pin/unpin running apps
- [ ] Folder stack fan/grid with drag-out
- [ ] Discord/WeChat/QQ badge adapters (real counts)
- [ ] Weather API integration
- [ ] Progress bar mirror from tray

### Sprint C — Ship gate

- [ ] Theme pack apply to dock/finder/clock
- [ ] Startup modes (registry / task scheduler)
- [ ] Hotkey editor UI
- [ ] Systematic `PARITY_CHECKLIST.md` QA on Windows 11 x64
- [ ] Add `Phase4FeatureTests.cs` for wiring regressions

---

## MyDockFinder feature map (website → BndzFinder)

| MyDockFinder feature | BndzFinder component | Sprint |
|----------------------|---------------------|--------|
| WinUI blur / Mica dock | `DockBarControl`, `GlassBackdropService` | A (visible) |
| Window preview on hover | `WindowPreviewCoordinator`, `WindowPreviewService` | B |
| Drag files to dock | `DockBarControl` DnD | B |
| Message badges (Discord, QQ…) | `BadgePollingService`, `BadgeAdapterRegistry` | B |
| Weather on dock | `WeatherService`, system weather item | B |
| Progress on icons | `ProgressBarMirrorService` | B |
| Minimize to dock (D3D11) | `MinimizeAnimationEngine`, `ShellHostWorker` | B |
| Dockmod background process | `BndzFinder.ShellHost` | A (must run) |
| Folder stacks | `FolderStackFlyout`, `FolderStackService` | B |
| Dark mode | `ThemeMode`, acrylic tints | A |

---

## Verify on Windows after each sprint

```powershell
.\run.cmd
```

1. Dock shows **running apps** with real icons
2. **Finder bar** visible at top with clock + tray icons
3. **Win+L** opens Launchpad with app icons
4. ShellHost running (minimize hook, tray mirror)
5. Log: `%LOCALAPPDATA%\BndzFinder\startup.log`

*Last updated: 2026-07-12 — Phase 4 sprint started*
