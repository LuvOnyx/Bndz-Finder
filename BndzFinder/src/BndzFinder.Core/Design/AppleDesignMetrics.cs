namespace BndzFinder.Core.Design;

/// <summary>
/// Layout metrics aligned with Apple Design Resources + macOS Sequoia/Tahoe dock chrome.
/// </summary>
public static class AppleDesignMetrics
{
    public const int IconCanvasSize = 1024;
    public const float IconContentInset = 100f;
    public const float IconContentSize = IconCanvasSize - (IconContentInset * 2);
    public const float IconSquircleRadiusRatio = 0.2237f;

    /// <summary>Tahoe-style dock pill corner radius (softer than Big Sur 24).</summary>
    public const double DockPillCornerRadius = 22;

    public const double DockPillBottomMargin = 8;
    public const double DockPillHorizontalMargin = 10;
    public const double DockStripMinHeight = 78;
    public const double DockIconPadding = 10;
    public const double DockIconDefaultSize = 52;
    public const double DockIconMaxSize = 78;

    /// <summary>True macOS menu bar height at 1x.</summary>
    public const double MenuBarHeight = 24;

    public const double MenuBarHorizontalPadding = 10;
    public const double MenuBarItemSpacing = 14;
    public const double MenuBarStatusSpacing = 10;
    public const double MenuBarFontSize = 13;
    public const double MenuBarAppTitleFontSize = 13;

    /// <summary>Dark frosted dock tint (#262626 family) matching Tahoe glass.</summary>
    public const string DockGlassTintDark = "#262626";
    public const string DockGlassTintLight = "#F5F5F7";
    public const string MenuBarTintDark = "#1C1C1E";
    public const string MenuBarTintLight = "#F5F5F7";
}
