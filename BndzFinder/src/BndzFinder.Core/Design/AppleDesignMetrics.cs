namespace BndzFinder.Core.Design;

/// <summary>
/// Layout metrics aligned with Apple Design Resources (macOS dock + app icon template).
/// </summary>
public static class AppleDesignMetrics
{
    public const int IconCanvasSize = 1024;
    public const float IconContentInset = 100f;
    public const float IconContentSize = IconCanvasSize - (IconContentInset * 2);
    public const float IconSquircleRadiusRatio = 0.2237f;

    public const double DockPillCornerRadius = 24;
    public const double DockPillBottomMargin = 10;
    public const double DockPillHorizontalMargin = 12;
    public const double DockStripMinHeight = 72;
}
