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
            SystemItem("finder", DockItemKind.SystemFinder, "Finder", 0),
            SystemItem("launchpad", DockItemKind.SystemLaunchpad, "Launchpad", 1),
            SystemItem("calendar", DockItemKind.SystemCalendar, "Calendar", 2),
            SystemItem("weather", DockItemKind.SystemWeather, "Weather", 3),
            SystemItem("prefs", DockItemKind.SystemPreferences, "System Settings", 4),
            new DockItem
            {
                Id = "sep-end",
                Kind = DockItemKind.Separator,
                TargetPath = "separator",
                DisplayName = "|",
                IsPinned = true,
                IsLocked = true,
                SortOrder = 50
            },
            SystemItem("trash", DockItemKind.SystemTrash, "Trash", 51)
        ];
    }

    public bool CanDragReorder(BndzFinderSettings settings, DockItem item) =>
        !settings.LockIcons && !item.IsLocked;

    private static DockItem SystemItem(string id, DockItemKind kind, string name, int order) => new()
    {
        Id = id,
        Kind = kind,
        TargetPath = kind.ToString(),
        DisplayName = name,
        IsPinned = true,
        SortOrder = order
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
