using BndzFinder.Core.Models;

namespace BndzFinder.Core.Settings;

public sealed class BndzFinderSettings
{
    public int SchemaVersion { get; set; } = 1;

    // Global
    public double DpiScale { get; set; } = 1.0;
    public ThemeMode ThemeMode { get; set; } = ThemeMode.Auto;
    public string AccentColor { get; set; } = "#0078D4";
    public string Language { get; set; } = "en";
    public StartupMode StartupMode { get; set; } = StartupMode.None;

    // MyDock
    public DockPosition DockPosition { get; set; } = DockPosition.Bottom;
    public DockDisplayMode DockDisplayMode { get; set; } = DockDisplayMode.Normal;
    public int IconSize { get; set; } = 48;
    public int IconSpace { get; set; } = 8;
    public int IconMaxSize { get; set; } = 72;
    public IconHoverEffect IconEffect { get; set; } = IconHoverEffect.Scale;
    public bool IconImmersion { get; set; }
    public bool LockIcons { get; set; }
    public int EdgePosition { get; set; }
    public int HideDockDelayMs { get; set; } = 400;
    public bool ShowDockActivationMouse { get; set; } = true;
    public bool ShowDockActivationBar { get; set; }
    public int ActivationBarWeight { get; set; } = 4;
    public int ActivationBarHeight { get; set; } = 3;
    public int ActivationBarOffset { get; set; }
    public string? DockMonitorName { get; set; }
    public bool PreviewOn { get; set; } = true;
    public int PreviewSize { get; set; } = 200;
    public int PreviewDelayMs { get; set; } = 300;
    public MinimizeEffect MinimizeEffect { get; set; } = MinimizeEffect.Genie;
    public double MinimizeAnimationSpeed { get; set; } = 1.0;
    public bool MinimizeDynamics { get; set; } = true;
    public bool MinimizeIntoAppIcon { get; set; }
    public bool HideTaskbarWhenDockShown { get; set; } = true;
    public bool HideTaskbarAllMonitors { get; set; }
    public bool AutoHideTaskbarAtStartup { get; set; } = true;
    public bool WindowsBorderMode { get; set; } = true;
    public bool CenterNewWindows { get; set; }
    public GlassEffect DockGlassEffect { get; set; } = GlassEffect.Acrylic;

    // MyFinder
    public bool FinderEnabled { get; set; } = true;
    public int FinderHeight { get; set; } = 28;
    public int FinderOffsetY { get; set; }
    public ThemeMode FinderThemeMode { get; set; } = ThemeMode.Auto;
    public bool FinderShowAllScreens { get; set; }
    public string? FinderMonitorName { get; set; }
    public int HideFinderDelayMs { get; set; } = 400;
    public bool FinderLockTrayButtons { get; set; }
    public int TrayIconWaitTimeMs { get; set; } = 500;
    public bool AlwaysShowAllTrayIcons { get; set; }
    public bool ShowCpu { get; set; } = true;
    public bool ShowGpu { get; set; } = true;
    public bool ShowMemory { get; set; } = true;
    public bool ShowDisk { get; set; } = true;
    public bool ShowNetwork { get; set; } = true;
    public bool ShowBattery { get; set; } = true;
    public bool ShowWeather { get; set; } = true;
    public bool ShowAudio { get; set; } = true;
    public bool ShowMicrophone { get; set; } = true;
    public bool ShowBluetooth { get; set; } = true;
    public bool ShowDisplay { get; set; } = true;
    public bool ShowKeyboard { get; set; } = true;
    public bool ShowMediaControl { get; set; } = true;
    public bool ShowLyrics { get; set; }
    public bool ShowNotifications { get; set; } = true;
    public string DateFormat { get; set; } = "ddd MMM d";
    public string TimeFormat { get; set; } = "h:mm tt";

    // Launchpad
    public bool LaunchpadEnabled { get; set; } = true;
    public int LaunchpadIconSize { get; set; } = 64;
    public int LaunchpadIconSpaceX { get; set; } = 32;
    public int LaunchpadIconSpaceY { get; set; } = 32;
    public bool LaunchpadHideLabels { get; set; }
    public bool LaunchpadHdIcons { get; set; } = true;
    public string LaunchpadIconSource { get; set; } = "startmenu";
    public string? LaunchpadMonitorName { get; set; }

    // Stage Manager
    public bool StageManagerEnabled { get; set; } = true;
    public bool ShowStageManagerInFinder { get; set; } = true;
    public bool StageManagerOnlyWindowList { get; set; }
    public int StageManagerWindowSize { get; set; } = 120;
    public int StageManagerWindowSpace { get; set; } = 8;
    public int StageManagerWindowCount { get; set; } = 5;
    public bool StageManagerWindowBlur { get; set; } = true;
    public bool ShowStageManagerWindowTitle { get; set; } = true;
    public string DesktopIconMode { get; set; } = "auto";
    public bool StageManagerMediaPauseResume { get; set; } = true;

    // Theming
    public double GlobalBlurValue { get; set; } = 0.6;
    public bool IconReflectionEnabled { get; set; } = true;
    public double IconReflectionOpacity { get; set; } = 0.35;
    public double IconReflectionBlur { get; set; } = 0.5;
    public string? ActiveIconTheme { get; set; }
    public string? ActiveDockSkin { get; set; }

    // Hotkeys & hot corners
    public HotkeyBinding DockHotkey { get; set; } = new() { Id = "hotkeyDock" };
    public HotkeyBinding FinderHotkey { get; set; } = new() { Id = "hotkeyfinder" };
    public HotkeyBinding LaunchpadHotkey { get; set; } = new() { Id = "hotkeypad", Modifiers = "Win", Key = "L" };
    public HotkeyBinding StageManagerHotkey { get; set; } = new() { Id = "stagemanager_hotkey" };
    public HotCornerBinding HotCorners { get; set; } = new();

    // Collections
    public List<DockItem> DockItems { get; set; } = [];
    public List<string> DockAppBlacklist { get; set; } = [];
    public List<string> LaunchpadBlacklist { get; set; } = [];
    public List<string> StageManagerBlacklist { get; set; } = [];
    public Dictionary<string, string> AppTrayIconRouting { get; set; } = new();
}
