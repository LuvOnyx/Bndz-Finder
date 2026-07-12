using BndzFinder.Core.Models;

namespace BndzFinder.Shell.Dock;

public interface IDockLayoutEngine
{
    IReadOnlyList<DockLayoutItem> ComputeLayout(
        IReadOnlyList<DockItem> items,
        double cursorPosition,
        int baseIconSize,
        int maxIconSize,
        int iconSpacing);
}

public sealed class DockLayoutItem
{
    public required DockItem Item { get; init; }
    public double X { get; init; }
    public double Y { get; init; }
    public double Size { get; init; }
    public double Scale { get; init; }
}

/// <summary>
/// macOS-style fisheye magnification layout engine.
/// </summary>
public sealed class FisheyeDockLayoutEngine : IDockLayoutEngine
{
    private const double MagnificationRadius = 120.0;
    private const double MagnificationCurve = 2.2;

    public IReadOnlyList<DockLayoutItem> ComputeLayout(
        IReadOnlyList<DockItem> items,
        double cursorPosition,
        int baseIconSize,
        int maxIconSize,
        int iconSpacing)
    {
        if (items.Count == 0) return [];

        var centers = new double[items.Count];
        var x = 0.0;
        for (var i = 0; i < items.Count; i++)
        {
            centers[i] = x + baseIconSize / 2.0;
            x += baseIconSize + iconSpacing;
        }

        var result = new List<DockLayoutItem>(items.Count);
        for (var i = 0; i < items.Count; i++)
        {
            var distance = Math.Abs(centers[i] - cursorPosition);
            var t = Math.Clamp(1.0 - distance / MagnificationRadius, 0.0, 1.0);
            var eased = Math.Pow(t, MagnificationCurve);
            var size = baseIconSize + (maxIconSize - baseIconSize) * eased;
            var scale = size / baseIconSize;
            var adjustedX = centers[i] - size / 2.0;

            result.Add(new DockLayoutItem
            {
                Item = items[i],
                X = adjustedX,
                Y = -(size - baseIconSize),
                Size = size,
                Scale = scale
            });
        }

        return result;
    }
}
