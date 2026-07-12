using BndzFinder.Core.Models;
using BndzFinder.Interop;

namespace BndzFinder.Shell.Services;

public sealed class RunningAppInfo
{
    public required string Id { get; init; }
    public required string ExePath { get; init; }
    public required string DisplayName { get; init; }
    public nint MainWindow { get; init; }
}

public interface IRunningAppSyncService
{
    IReadOnlyList<RunningAppInfo> GetRunningApps(IReadOnlyList<string> blacklist);
    IReadOnlyList<DockItem> MergeDockItems(
        IReadOnlyList<DockItem> pinnedItems,
        IReadOnlyList<RunningAppInfo> runningApps,
        out HashSet<string> runningIds);
}

public sealed class RunningAppSyncService : IRunningAppSyncService
{
    private static readonly HashSet<string> ExcludedProcessNames =
    [
        "BndzFinder.App",
        "BndzFinder.ShellHost",
        "dock",
        "Dockmod",
        "Mydock",
        "explorer",
        "SearchHost",
        "ShellExperienceHost",
        "StartMenuExperienceHost",
        "SystemSettings",
        "ApplicationFrameHost"
    ];

    public IReadOnlyList<RunningAppInfo> GetRunningApps(IReadOnlyList<string> blacklist)
    {
        if (!OperatingSystem.IsWindows()) return [];

        var excluded = blacklist.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var results = new List<RunningAppInfo>();

        foreach (var window in WindowEnumerationService.GetOpenWindows([]))
        {
            if (!TryGetProcessInfo(window.Hwnd, out var exePath, out var processName)) continue;
            if (ExcludedProcessNames.Contains(processName)) continue;
            if (excluded.Contains(processName) || excluded.Contains(Path.GetFileName(exePath))) continue;
            if (!seenPaths.Add(exePath)) continue;

            results.Add(new RunningAppInfo
            {
                Id = $"running:{exePath.ToLowerInvariant()}",
                ExePath = exePath,
                DisplayName = Path.GetFileNameWithoutExtension(exePath),
                MainWindow = window.Hwnd
            });
        }

        return results.OrderBy(a => a.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public IReadOnlyList<DockItem> MergeDockItems(
        IReadOnlyList<DockItem> pinnedItems,
        IReadOnlyList<RunningAppInfo> runningApps,
        out HashSet<string> runningIds)
    {
        runningIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var merged = pinnedItems.ToList();
        var pinnedPaths = pinnedItems
            .Where(i => i.Kind is DockItemKind.Application or DockItemKind.File or DockItemKind.Folder)
            .Select(i => i.TargetPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var sortBase = merged.Count > 0 ? merged.Max(i => i.SortOrder) + 1 : 0;

        foreach (var app in runningApps)
        {
            var pinned = merged.FirstOrDefault(i =>
                i.Kind is DockItemKind.Application or DockItemKind.File
                && i.TargetPath.Equals(app.ExePath, StringComparison.OrdinalIgnoreCase));

            if (pinned is not null)
            {
                runningIds.Add(pinned.Id);
                continue;
            }

            if (pinnedPaths.Contains(app.ExePath)) continue;

            merged.Add(new DockItem
            {
                Id = app.Id,
                Kind = DockItemKind.Application,
                TargetPath = app.ExePath,
                DisplayName = app.DisplayName,
                IsPinned = false,
                SortOrder = sortBase++
            });
            runningIds.Add(app.Id);
        }

        return merged.OrderBy(i => i.SortOrder).ToList();
    }

    private static bool TryGetProcessInfo(nint hwnd, out string exePath, out string processName)
    {
        exePath = string.Empty;
        processName = string.Empty;
        if (!OperatingSystem.IsWindows()) return false;

        _ = GetWindowThreadProcessId(hwnd, out var pid);
        if (pid == 0) return false;

        try
        {
            using var process = System.Diagnostics.Process.GetProcessById((int)pid);
            processName = process.ProcessName;
            exePath = process.MainModule?.FileName ?? string.Empty;
            return !string.IsNullOrWhiteSpace(exePath);
        }
        catch
        {
            return false;
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);
}
