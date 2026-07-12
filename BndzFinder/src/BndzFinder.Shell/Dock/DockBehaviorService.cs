using BndzFinder.Core.Models;
using BndzFinder.Core.Settings;

namespace BndzFinder.Shell.Dock;

public interface IDockBehaviorService
{
    bool ShouldShowDock(BndzFinderSettings settings, bool pointerNearEdge, bool hasEligibleWindows);
    IReadOnlyList<DockItem> EnsureDefaultItems(BndzFinderSettings settings);
    bool CanDragReorder(BndzFinderSettings settings, DockItem item);
}

public sealed class DockBehaviorService : IDockBehaviorService
{
    public bool ShouldShowDock(BndzFinderSettings settings, bool pointerNearEdge, bool hasEligibleWindows) =>
        settings.DockDisplayMode switch
        {
            DockDisplayMode.AlwaysHidden => false,
            DockDisplayMode.HotkeyOnly => false,
            DockDisplayMode.AlwaysShow => true,
            DockDisplayMode.AutoHide => pointerNearEdge,
            DockDisplayMode.SmartHide => hasEligibleWindows || pointerNearEdge,
            _ => true
        };

    public IReadOnlyList<DockItem> EnsureDefaultItems(BndzFinderSettings settings)
    {
        if (settings.DockItems.Count > 0) return settings.DockItems;

        return
        [
            SystemItem("finder", DockItemKind.SystemFinder, "Finder"),
            SystemItem("launchpad", DockItemKind.SystemLaunchpad, "Launchpad"),
            SystemItem("calendar", DockItemKind.SystemCalendar, "Calendar"),
            SystemItem("trash", DockItemKind.SystemTrash, "Trash"),
            SystemItem("weather", DockItemKind.SystemWeather, "Weather"),
            SystemItem("prefs", DockItemKind.SystemPreferences, "Preferences")
        ];
    }

    public bool CanDragReorder(BndzFinderSettings settings, DockItem item) =>
        !settings.LockIcons && !item.IsLocked;

    private static DockItem SystemItem(string id, DockItemKind kind, string name) => new()
    {
        Id = id,
        Kind = kind,
        TargetPath = kind.ToString(),
        DisplayName = name,
        IsPinned = true,
        SortOrder = (int)kind
    };
}

public sealed class WindowPreviewCoordinator
{
    private readonly int _delayMs;
    private readonly int _previewSize;
    private CancellationTokenSource? _hoverCts;

    public WindowPreviewCoordinator(int delayMs = 300, int previewSize = 200)
    {
        _delayMs = delayMs;
        _previewSize = previewSize;
    }

    public event Action<long>? PreviewShowRequested;
    public event Action? PreviewHideRequested;

    public async Task OnIconHoveredAsync(long hwnd, CancellationToken ct)
    {
        _hoverCts?.Cancel();
        _hoverCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        try
        {
            await Task.Delay(_delayMs, _hoverCts.Token).ConfigureAwait(false);
            PreviewShowRequested?.Invoke(hwnd);
        }
        catch (TaskCanceledException) { }
    }

    public void OnIconExited()
    {
        _hoverCts?.Cancel();
        PreviewHideRequested?.Invoke();
    }

    public int PreviewSize => _previewSize;
}
