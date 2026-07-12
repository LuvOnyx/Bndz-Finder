# Bndz-Finder ↔ MyDockFinder Parity Checklist

This document tracks feature parity between **Bndz-Finder** and **MyDockFinder**. Use it during development, QA, and release planning to confirm that each MyDockFinder capability has a corresponding implementation, setting, and user-visible behavior in Bndz-Finder.

**How to use**

- Check an item (`- [x]`) only when the feature is implemented, wired to settings/IPC where applicable, and manually verified on Windows 11 x64.
- Reference the matching `BndzFinderSettings` key or component when auditing (see `src/BndzFinder.Core/Settings/BndzFinderSettings.cs`).
- Items intentionally exclude Steam Workshop distribution, Steam-only assets, and Workshop subscription flows.

**Status legend**

| Symbol | Meaning |
|--------|---------|
| `- [ ]` | Not verified / not complete |
| `- [x]` | Parity confirmed |

---

## MyDock

### Position, layout, and display

- [ ] Dock can be placed on bottom edge (`DockPosition.Bottom`)
- [ ] Dock can be placed on left edge (`DockPosition.Left`)
- [ ] Dock can be placed on right edge (`DockPosition.Right`)
- [ ] Dock can be placed on top edge (`DockPosition.Top`)
- [ ] Edge offset from screen border is configurable (`EdgePosition`)
- [ ] Normal display mode shows dock persistently (`DockDisplayMode.Normal`)
- [ ] Auto-hide mode hides dock until pointer nears edge (`DockDisplayMode.AutoHide`)
- [ ] Smart-hide mode hides dock when no eligible windows are open (`DockDisplayMode.SmartHide`)
- [ ] Hotkey-only mode shows dock only via hotkey (`DockDisplayMode.HotkeyOnly`)
- [ ] Always-show mode keeps dock visible regardless of focus (`DockDisplayMode.AlwaysShow`)
- [ ] Always-hidden mode keeps dock hidden until explicitly invoked (`DockDisplayMode.AlwaysHidden`)
- [ ] Hide delay after pointer leaves dock is configurable (`HideDockDelayMs`)
- [ ] Mouse-based dock activation at screen edge works (`ShowDockActivationMouse`)
- [ ] Visual activation bar at screen edge can be enabled (`ShowDockActivationBar`)
- [ ] Activation bar thickness is configurable (`ActivationBarWeight`)
- [ ] Activation bar length/height is configurable (`ActivationBarHeight`)
- [ ] Activation bar offset from edge is configurable (`ActivationBarOffset`)

### Icons, fisheye, and interaction

- [ ] Base icon size is configurable (`IconSize`)
- [ ] Maximum fisheye magnification size is configurable (`IconMaxSize`)
- [ ] Spacing between icons is configurable (`IconSpace`)
- [ ] Icon hover effect: none (`IconHoverEffect.None`)
- [ ] Icon hover effect: scale / fisheye (`IconHoverEffect.Scale`)
- [ ] Icon hover effect: select highlight (`IconHoverEffect.Select`)
- [ ] Icon hover effect: light glow (`IconHoverEffect.Light`)
- [ ] Icon hover effect: scale + light (`IconHoverEffect.ScaleLight`)
- [ ] Icon immersion mode enlarges nearby icons (`IconImmersion`)
- [ ] Dock icons can be locked against drag reorder (`LockIcons`)
- [ ] Running applications show indicator under icon
- [ ] Minimized windows can appear as dock items (`DockItemKind.MinimizedWindow`)
- [ ] Separator items can be inserted in dock (`DockItemKind.Separator`)
- [ ] Drag-and-drop reorder of dock items persists to settings
- [ ] Pin application to dock from running process
- [ ] Unpin / remove dock item
- [ ] Per-app dock display mode override (`DockItem.PerAppDisplayMode`)
- [ ] Custom icon path per dock item (`DockItem.CustomIconPath`)
- [ ] Locked per-item flag prevents accidental edits (`DockItem.IsLocked`)

### Folder stacks and special dock items

- [ ] Folder stack: fan view (`FolderStackView.Fan`)
- [ ] Folder stack: grid view (`FolderStackView.Grid`)
- [ ] Folder stack: list view (`FolderStackView.List`)
- [ ] Folder stack: automatic view selection (`FolderStackView.Automatic`)
- [ ] Folder sort by name (`FolderSortMode.Name`)
- [ ] Folder sort by date added (`FolderSortMode.DateAdded`)
- [ ] Folder sort by date modified (`FolderSortMode.DateModified`)
- [ ] Folder sort by date created (`FolderSortMode.DateCreated`)
- [ ] Folder sort by kind (`FolderSortMode.Kind`)
- [ ] Show folder as stack vs expanded fan (`FolderStackOptions.ShowAsStack`)
- [ ] File shortcuts can be pinned to dock (`DockItemKind.File`)
- [ ] Folder shortcuts can be pinned to dock (`DockItemKind.Folder`)
- [ ] System Finder shortcut on dock (`DockItemKind.SystemFinder`)
- [ ] System Launchpad shortcut on dock (`DockItemKind.SystemLaunchpad`)
- [ ] System Calendar shortcut on dock (`DockItemKind.SystemCalendar`)
- [ ] System Trash shortcut on dock (`DockItemKind.SystemTrash`)
- [ ] System Weather shortcut on dock (`DockItemKind.SystemWeather`)
- [ ] System Preferences shortcut on dock (`DockItemKind.SystemPreferences`)

### Window preview, minimize, and shell integration

- [ ] Window preview on hover is enabled by default (`PreviewOn`)
- [ ] Preview thumbnail size is configurable (`PreviewSize`)
- [ ] Preview show delay is configurable (`PreviewDelayMs`)
- [ ] Minimize effect: Genie animation (`MinimizeEffect.Genie`)
- [ ] Minimize effect: Scale animation (`MinimizeEffect.Scale`)
- [ ] Minimize effect: ScaleDX animation (`MinimizeEffect.ScaleDx`)
- [ ] Minimize effect: Suck animation (`MinimizeEffect.Suck`)
- [ ] Minimize animation speed multiplier (`MinimizeAnimationSpeed`)
- [ ] Minimize dynamics / physics-style motion (`MinimizeDynamics`)
- [ ] Minimize into originating app icon (`MinimizeIntoAppIcon`)
- [ ] ShellHost intercepts minimize via WinEvent/CBT hooks
- [ ] Restore animation reverses minimize path via IPC (`RestoreRequested`)
- [ ] AppBar registration reserves dock screen space correctly
- [ ] Hide Windows taskbar when dock is shown (`HideTaskbarWhenDockShown`)
- [ ] Hide taskbar on all monitors option (`HideTaskbarAllMonitors`)
- [ ] Auto-hide taskbar at startup (`AutoHideTaskbarAtStartup`)
- [ ] Windows border / title-bar styling mode (`WindowsBorderMode`)
- [ ] Center newly opened windows on screen (`CenterNewWindows`)

### Visual effects, badges, and dock chrome

- [ ] Glass backdrop: translucent (`GlassEffect.Translucent`)
- [ ] Glass backdrop: acrylic (`GlassEffect.Acrylic`)
- [ ] Glass backdrop: mica (`GlassEffect.Mica`)
- [ ] Glass backdrop: liquid glass (`GlassEffect.LiquidGlass`)
- [ ] D3D11 compositor renders dock background and icons
- [ ] Mac-style icon pipeline normalizes app icons (`MacStyleIconPipeline`)
- [ ] Fisheye layout engine computes magnified positions (`FisheyeDockLayoutEngine`)
- [ ] App blacklist excludes processes from dock (`DockAppBlacklist`)
- [ ] Progress bar mirror on dock item for supported apps
- [ ] Weather widget on dock shows current conditions
- [ ] Weather widget shows short forecast
- [ ] Unread badge count on Discord icon
- [ ] Unread badge count on WeChat icon
- [ ] Unread badge count on QQ / TIM / DingTalk / AliWangwang / YY

---

## MyFinder

### Bar presence, layout, and theming

- [ ] Finder menu bar can be enabled/disabled (`FinderEnabled`)
- [ ] Finder bar height is configurable (`FinderHeight`)
- [ ] Vertical offset from top edge (`FinderOffsetY`)
- [ ] Finder light theme mode (`FinderThemeMode.Light`)
- [ ] Finder dark theme mode (`FinderThemeMode.Dark`)
- [ ] Finder auto theme follows Windows (`FinderThemeMode.Auto`)
- [ ] Finder appears on all screens (`FinderShowAllScreens`)
- [ ] Finder can be restricted to named monitor (`FinderMonitorName`)
- [ ] Auto-hide delay for finder bar (`HideFinderDelayMs`)
- [ ] Finder bar uses AppBar / top-edge reservation
- [ ] Finder glass/blur matches global theme settings

### Clock, date, and localization hooks

- [ ] Clock displays with configurable time format (`TimeFormat`)
- [ ] Date displays with configurable date format (`DateFormat`)
- [ ] Clock updates in real time (1s polling or event-driven)
- [ ] Calendar popover opens from date/clock area
- [ ] Calendar skin from active theme pack applies correctly

### System status widgets (left / center regions)

- [ ] CPU usage widget (`ShowCpu`)
- [ ] GPU usage widget (`ShowGpu`)
- [ ] Memory / RAM usage widget (`ShowMemory`)
- [ ] Disk usage widget (`ShowDisk`)
- [ ] Network throughput widget (`ShowNetwork`)
- [ ] Battery level widget (`ShowBattery`)
- [ ] Weather summary widget (`ShowWeather`)
- [ ] Audio output device / volume widget (`ShowAudio`)
- [ ] Microphone mute / level widget (`ShowMicrophone`)
- [ ] Bluetooth status widget (`ShowBluetooth`)
- [ ] Display / monitor brightness widget (`ShowDisplay`)
- [ ] Keyboard layout / input method widget (`ShowKeyboard`)
- [ ] Media playback controls widget (`ShowMediaControl`)
- [ ] Lyrics overlay for supported players (`ShowLyrics`)
- [ ] Notifications center toggle widget (`ShowNotifications`)
- [ ] Each widget can be individually hidden via preferences
- [ ] Widget click opens corresponding Windows quick settings or panel

### System tray proxy

- [ ] ShellHost mirrors hidden tray icons (`TrayIconMirrorService`)
- [ ] Tray icons appear in Finder right region
- [ ] Tray icon tooltip preserved (`TrayProxyItem.Tooltip`)
- [ ] Tray icon image data preserved (`TrayProxyItem.IconData`)
- [ ] Tray icon wait/poll interval (`TrayIconWaitTimeMs`)
- [ ] Always show all tray icons mode (`AlwaysShowAllTrayIcons`)
- [ ] Tray buttons can be locked from reorder (`FinderLockTrayButtons`)
- [ ] Per-app tray icon routing map (`AppTrayIconRouting`)
- [ ] Click on mirrored tray icon forwards action to original
- [ ] Overflow / hidden tray icons accessible from Finder

### Stage Manager integration in Finder

- [ ] Stage Manager strip can embed in Finder bar (`ShowStageManagerInFinder`)
- [ ] Stage Manager launcher control visible when enabled
- [ ] Finder remains usable when Stage Manager disabled globally

---

## Launchpad

### Visibility and invocation

- [ ] Launchpad feature toggle (`LaunchpadEnabled`)
- [ ] Full-screen overlay opens on configured hotkey
- [ ] Full-screen overlay opens from dock system item
- [ ] Launchpad dismisses on Escape / outside click
- [ ] Launchpad respects monitor assignment (`LaunchpadMonitorName`)

### App grid and catalog

- [ ] App catalog loads from Start Menu (`LaunchpadIconSource = startmenu`)
- [ ] App catalog loads from Desktop shortcuts (`LaunchpadIconSource = desktop`)
- [ ] Icons render at configured size (`LaunchpadIconSize`)
- [ ] Horizontal icon spacing (`LaunchpadIconSpaceX`)
- [ ] Vertical icon spacing (`LaunchpadIconSpaceY`)
- [ ] App labels can be hidden (`LaunchpadHideLabels`)
- [ ] HD / high-DPI icon extraction (`LaunchpadHdIcons`)
- [ ] Paged grid with smooth horizontal navigation
- [ ] Apps sorted alphabetically by default
- [ ] Blacklisted apps excluded from grid (`LaunchpadBlacklist`)

### Search and launch

- [ ] Search field filters apps by name (case-insensitive)
- [ ] Search clears when Launchpad closes
- [ ] Single click launches application
- [ ] Launch uses shell execute / correct working directory
- [ ] Empty search shows full catalog
- [ ] No results state displayed for unmatched query

---

## Stage Manager

### Core window strip

- [ ] Stage Manager feature toggle (`StageManagerEnabled`)
- [ ] Window list-only mode without desktop thumbnails (`StageManagerOnlyWindowList`)
- [ ] Open windows enumerated via ShellHost IPC (`WindowListUpdated`)
- [ ] Thumbnail width/height (`StageManagerWindowSize`)
- [ ] Spacing between thumbnails (`StageManagerWindowSpace`)
- [ ] Maximum visible window count (`StageManagerWindowCount`)
- [ ] Thumbnail live preview / DWM capture
- [ ] Window title shown under thumbnail (`ShowStageManagerWindowTitle`)
- [ ] Thumbnail background blur (`StageManagerWindowBlur`)
- [ ] Click thumbnail focuses target window
- [ ] Close button / gesture closes window from strip
- [ ] Blacklisted apps excluded (`StageManagerBlacklist`)

### Desktop and media behavior

- [ ] Desktop icon visibility mode (`DesktopIconMode = auto`)
- [ ] Desktop icon visibility mode manual show
- [ ] Desktop icon visibility mode manual hide
- [ ] Switching windows updates active highlight
- [ ] Media pause/resume on window focus change (`StageManagerMediaPauseResume`)
- [ ] Strip scrolls or paginates when window count exceeds max
- [ ] Strip updates within 500ms of window open/close

---

## Hotkeys

- [ ] Global dock show/hide hotkey (`DockHotkey`)
- [ ] Global finder show/hide hotkey (`FinderHotkey`)
- [ ] Global Launchpad hotkey (default Win+L) (`LaunchpadHotkey`)
- [ ] Global Stage Manager hotkey (`StageManagerHotkey`)
- [ ] Hotkeys registered in ShellHost (`GlobalHotkeyService`)
- [ ] Hotkey press delivered to App via IPC (`HotkeyPressed`)
- [ ] Hotkey bindings persist across restart
- [ ] Hotkey UI allows modifier selection (Ctrl, Alt, Shift, Win)
- [ ] Hotkey UI prevents duplicate bindings
- [ ] Hotkey works when desktop focused
- [ ] Hotkey works when full-screen app focused
- [ ] Hotkey can be cleared / disabled (empty binding)
- [ ] Hotkey respects elevation / low-level hook permissions

---

## Hot Corners

- [ ] Bottom-left hot corner configurable (`HotCorners.BottomLeft`)
- [ ] Bottom-right hot corner configurable (`HotCorners.BottomRight`)
- [ ] Hot corner action: none (`HotCornerAction.None`)
- [ ] Hot corner action: open Stage Manager (`HotCornerAction.StageManager`)
- [ ] Hot corner action: open Launchpad (`HotCornerAction.Launchpad`)
- [ ] Hot corner action: show desktop (`HotCornerAction.ShowDesktop`)
- [ ] Hot corner action: display sleep (`HotCornerAction.DisplaySleep`)
- [ ] Hot corner action: open Start Menu (`HotCornerAction.StartMenu`)
- [ ] Hot corner action: open Action Center (`HotCornerAction.ActionCenter`)
- [ ] Hot corner action: lock screen (`HotCornerAction.LockScreen`)
- [ ] Hot corner action: open Windows Widgets (`HotCornerAction.WindowsWidgets`)
- [ ] Hot corner trigger delay prevents accidental activation
- [ ] Hot corner detection runs in ShellHost / background service
- [ ] Hot corner settings survive settings save/load cycle

---

## Notifications

- [ ] Notification center toggle visible in Finder when enabled (`ShowNotifications`)
- [ ] Click opens Windows notification center / Action Center
- [ ] Unread notification badge on Finder icon (if applicable)
- [ ] Toast notifications do not break dock/finder z-order
- [ ] Focus-assist / quiet hours status reflected in UI
- [ ] Notification sounds respect system mute state
- [ ] App-specific notification badges on dock icons (Discord, WeChat, etc.)
- [ ] Clearing notifications updates badge counts
- [ ] Do-not-disturb quick toggle from Finder
- [ ] Notification history accessible from Finder widget

---

## Theming

- [ ] Global theme mode: light (`ThemeMode.Light`)
- [ ] Global theme mode: dark (`ThemeMode.Dark`)
- [ ] Global theme mode: auto (`ThemeMode.Auto`)
- [ ] Accent color picker (`AccentColor`)
- [ ] Global blur intensity (`GlobalBlurValue`)
- [ ] Icon reflection enabled (`IconReflectionEnabled`)
- [ ] Icon reflection opacity (`IconReflectionOpacity`)
- [ ] Icon reflection blur (`IconReflectionBlur`)
- [ ] Active icon theme pack id (`ActiveIconTheme`)
- [ ] Active dock skin id (`ActiveDockSkin`)
- [ ] Theme pack import from ZIP (`ThemePackService.ImportAsync`)
- [ ] Theme pack export to ZIP (`ThemePackService.ExportAsync`)
- [ ] Installed theme packs listed (`ThemePackService.ListInstalled`)
- [ ] Theme manifest fields: dock skin, icon shell, progress bar
- [ ] Theme manifest fields: run indicator, delimiter, time skin, calendar skin
- [ ] Dock skin applies to dock chrome
- [ ] Icon shell applies to application icons
- [ ] Time skin applies to Finder clock
- [ ] Calendar skin applies to calendar popover

---

## Preferences

### Application shell

- [ ] Preferences window opens from dock system item
- [ ] Preferences window opens from Finder menu
- [ ] Single-instance guard prevents duplicate preference windows
- [ ] Section: General (`PreferencesSection.General`)
- [ ] Section: Appearance (`PreferencesSection.Appearance`)
- [ ] Section: System Icon Tray (`PreferencesSection.SystemIconTray`)
- [ ] Section: Screen (`PreferencesSection.Screen`)
- [ ] Section: Look and Behavior (`PreferencesSection.LookAndBehavior`)
- [ ] Section: Launchpad (`PreferencesSection.Launchpad`)
- [ ] Section: Window Animations (`PreferencesSection.WindowAnimations`)
- [ ] Section: Audio / Display / Network (`PreferencesSection.AudioDisplayNetwork`)
- [ ] Section: Advanced — hot corners and hotkeys (`PreferencesSection.Advanced`)
- [ ] Section: Themes (`PreferencesSection.Themes`)

### Settings persistence and reset

- [ ] Save writes atomically to `%APPDATA%\BndzFinder\settings.json`
- [ ] Load on startup via `ISettingsService.LoadAsync`
- [ ] Settings change event refreshes live UI (`SettingsChanged`)
- [ ] Reset dock component to defaults (`ResetComponentAsync("dock")`)
- [ ] Reset finder component to defaults (`ResetComponentAsync("finder")`)
- [ ] Reset launchpad component to defaults (`ResetComponentAsync("launchpad")`)
- [ ] Reset stage manager component to defaults (`ResetComponentAsync("stagemanager")`)
- [ ] Reset theme component to defaults (`ResetComponentAsync("theme")`)
- [ ] Reset all components to defaults (`ResetComponentAsync("all")`)
- [ ] Backup settings and dock icons to ZIP (`BackupService.BackupAsync`)
- [ ] Restore settings from ZIP (`BackupService.RestoreAsync`)
- [ ] Schema version field migrates older settings (`SchemaVersion`)
- [ ] DPI scale setting applied (`DpiScale`)

---

## Startup

- [ ] Startup mode: none / manual only (`StartupMode.None`)
- [ ] Startup mode: Windows Registry Run key (`StartupMode.Registry`)
- [ ] Startup mode: Task Scheduler task (`StartupMode.TaskScheduler`)
- [ ] Startup mode: Windows Service (`StartupMode.WindowsService`)
- [ ] `StartupService.ConfigureAsync` applies selected mode
- [ ] ShellHost process starts with App (Dockmod equivalent)
- [ ] ShellHost IPC pipe `BndzFinder.ShellHost` connects on launch
- [ ] Single-instance mutex prevents duplicate App instances
- [ ] Second instance activates existing window instead of relaunching
- [ ] Clean shutdown sends `Shutdown` IPC to ShellHost
- [ ] Auto-hide taskbar applied at startup when configured
- [ ] Settings loaded before dock/finder windows shown

---

## Multi-monitor

- [ ] Dock can be assigned to named monitor (`DockMonitorName`)
- [ ] Finder can be assigned to named monitor (`FinderMonitorName`)
- [ ] Launchpad can be assigned to named monitor (`LaunchpadMonitorName`)
- [ ] Finder show-on-all-screens spans every display (`FinderShowAllScreens`)
- [ ] Per-monitor DPI scaling handled (`DpiScale` + per-monitor v2)
- [ ] Dock AppBar registers on correct monitor rectangle
- [ ] Finder AppBar registers on correct monitor rectangle
- [ ] Moving pointer between monitors does not orphan dock
- [ ] Hot corners evaluated per monitor geometry
- [ ] Stage Manager thumbnails scoped to active monitor (or global option)
- [ ] Taskbar hide option respects all-monitors setting (`HideTaskbarAllMonitors`)
- [ ] Monitor name list populated from Windows display API
- [ ] Fallback behavior when named monitor unplugged
- [ ] Launchpad opens on cursor monitor when no assignment set
- [ ] Window centering respects monitor containing cursor

---

## Localization

- [ ] Language setting persists (`Language`)
- [ ] English (`en`) strings complete
- [ ] Spanish (`es`) strings complete
- [ ] Portuguese Brazil (`pt-BR`) strings complete
- [ ] Chinese (`zh`) strings complete
- [ ] Russian (`ru`) strings complete
- [ ] Japanese (`ja`) strings complete
- [ ] Korean (`ko`) strings complete
- [ ] French (`fr`) strings complete
- [ ] German (`de`) strings complete
- [ ] Italian (`it`) strings complete
- [ ] Ukrainian (`uk`) strings complete
- [ ] Polish (`pl`) strings complete
- [ ] Turkish (`tr`) strings complete
- [ ] Arabic (`ar`) strings complete with RTL layout
- [ ] Hindi (`hi`) strings complete
- [ ] Thai (`th`) strings complete
- [ ] Vietnamese (`vi`) strings complete
- [ ] Indonesian (`id`) strings complete
- [ ] Dutch (`nl`) strings complete
- [ ] Swedish (`sv`) strings complete
- [ ] Danish (`da`) strings complete
- [ ] Norwegian (`no`) strings complete
- [ ] Finnish (`fi`) strings complete
- [ ] Czech (`cs`) strings complete
- [ ] Hungarian (`hu`) strings complete
- [ ] `Loc.Get` falls back to English for missing keys
- [ ] Preferences section titles localized (`Section.*` keys)
- [ ] Dock preference labels localized (`Dock.*` keys)
- [ ] Language change applies without full reinstall
- [ ] Date/time formats respect locale in Finder clock

---

## Cross-cutting IPC and orchestration

- [ ] `Ping` / `Pong` health check over named pipe
- [ ] `MinimizeRequested` sent from App to ShellHost
- [ ] `MinimizeCompleted` sent from ShellHost to App
- [ ] `TrayIconsUpdated` triggers Finder tray refresh
- [ ] `WindowListUpdated` triggers Stage Manager refresh
- [ ] Orchestrator loads settings before showing UI (`ShellOrchestrator`)

---

## Summary

| Section | Items |
|---------|------:|
| MyDock | 78 |
| MyFinder | 42 |
| Launchpad | 22 |
| Stage Manager | 19 |
| Hotkeys | 13 |
| Hot Corners | 14 |
| Notifications | 10 |
| Theming | 20 |
| Preferences | 25 |
| Startup | 12 |
| Multi-monitor | 15 |
| Localization | 30 |
| Cross-cutting IPC | 6 |
| **Total** | **296** |

*Last updated: 2026-07-12*
