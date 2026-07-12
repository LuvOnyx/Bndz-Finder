namespace BndzFinder.Core.Models;

public enum DockPosition
{
    Bottom = 0,
    Left = 1,
    Right = 2,
    Top = 3
}

public enum DockDisplayMode
{
    Normal = 0,
    AutoHide = 1,
    SmartHide = 2,
    HotkeyOnly = 3,
    AlwaysShow = 4,
    AlwaysHidden = 5
}

public enum IconHoverEffect
{
    None = 0,
    Scale = 1,
    Select = 2,
    Light = 3,
    ScaleLight = 4
}

public enum MinimizeEffect
{
    Genie = 0,
    Scale = 1,
    ScaleDx = 2,
    Suck = 3
}

public enum FolderStackView
{
    Automatic = 0,
    Fan = 1,
    Grid = 2,
    List = 3
}

public enum FolderSortMode
{
    Name = 0,
    DateAdded = 1,
    DateModified = 2,
    DateCreated = 3,
    Kind = 4
}

public enum HotCornerAction
{
    None = 0,
    StageManager = 1,
    Launchpad = 2,
    ShowDesktop = 3,
    DisplaySleep = 4,
    StartMenu = 5,
    ActionCenter = 6,
    LockScreen = 7,
    WindowsWidgets = 8
}

public enum StartupMode
{
    None = 0,
    Registry = 1,
    TaskScheduler = 2,
    WindowsService = 3
}

public enum ThemeMode
{
    Light = 0,
    Dark = 1,
    Auto = 2
}

public enum GlassEffect
{
    Translucent = 0,
    Acrylic = 1,
    Mica = 2,
    LiquidGlass = 3
}

public enum DockItemKind
{
    Application = 0,
    File = 1,
    Folder = 2,
    Separator = 3,
    SystemFinder = 4,
    SystemLaunchpad = 5,
    SystemCalendar = 6,
    SystemTrash = 7,
    SystemWeather = 8,
    SystemPreferences = 9,
    MinimizedWindow = 10
}

public sealed class DockItem
{
    public required string Id { get; init; }
    public DockItemKind Kind { get; init; }
    public required string TargetPath { get; init; }
    public string? DisplayName { get; set; }
    public string? CustomIconPath { get; set; }
    public bool IsLocked { get; set; }
    public bool IsPinned { get; set; }
    public int SortOrder { get; set; }
    public FolderStackOptions? FolderOptions { get; set; }
    public DockDisplayMode? PerAppDisplayMode { get; set; }
}

public sealed class FolderStackOptions
{
    public FolderStackView View { get; set; } = FolderStackView.Automatic;
    public FolderSortMode Sort { get; set; } = FolderSortMode.Name;
    public bool ShowAsStack { get; set; }
}

public sealed class HotkeyBinding
{
    public required string Id { get; init; }
    public string? Modifiers { get; set; }
    public string? Key { get; set; }
}

public sealed class HotCornerBinding
{
    public HotCornerAction BottomLeft { get; set; }
    public HotCornerAction BottomRight { get; set; }
}
