using BndzFinder.Core.Models;

namespace BndzFinder.Shell.Dock;

public sealed record FolderStackEntry
{
    public required string Path { get; init; }
    public required string Name { get; init; }
    public bool IsDirectory { get; init; }
    public int Index { get; init; }
    public double FanAngle { get; init; }
    public double FanRadius { get; init; }
    public double GridX { get; init; }
    public double GridY { get; init; }
}

public static class FolderStackLayoutHelper
{
    public static FolderStackView ResolveView(FolderStackView requested, int itemCount) =>
        requested == FolderStackView.Automatic
            ? itemCount <= 9 ? FolderStackView.Fan : FolderStackView.Grid
            : requested;
}

public sealed class FolderStackService
{
    public IReadOnlyList<string> GetContents(string folderPath, FolderSortMode sort)
    {
        if (!Directory.Exists(folderPath)) return [];
        var files = Directory.EnumerateFileSystemEntries(folderPath);
        return sort switch
        {
            FolderSortMode.Name => files.OrderBy(Path.GetFileName).ToList(),
            FolderSortMode.DateModified => files.OrderByDescending(File.GetLastWriteTimeUtc).ToList(),
            FolderSortMode.DateCreated => files.OrderByDescending(File.GetCreationTimeUtc).ToList(),
            FolderSortMode.Kind => files.OrderBy(GetKind).ToList(),
            _ => files.ToList()
        };
    }

    public IReadOnlyList<FolderStackEntry> BuildLayout(IReadOnlyList<string> paths, FolderStackView view)
    {
        var effectiveView = FolderStackLayoutHelper.ResolveView(view, paths.Count);
        var entries = paths.Select(p => new FolderStackEntry
        {
            Path = p,
            Name = Path.GetFileName(p) ?? p,
            IsDirectory = Directory.Exists(p)
        }).ToList();

        return effectiveView switch
        {
            FolderStackView.Fan => LayoutFan(entries),
            FolderStackView.Grid => LayoutGrid(entries),
            FolderStackView.List => entries,
            _ => entries
        };
    }

    private static IReadOnlyList<FolderStackEntry> LayoutFan(List<FolderStackEntry> entries)
    {
        var count = entries.Count;
        if (count == 0) return entries;
        var startAngle = -60.0;
        var endAngle = 60.0;
        var step = count == 1 ? 0 : (endAngle - startAngle) / (count - 1);
        for (var i = 0; i < count; i++)
        {
            var angle = startAngle + step * i;
            entries[i] = entries[i] with { FanAngle = angle, FanRadius = 80 + i * 4 };
        }
        return entries;
    }

    private static IReadOnlyList<FolderStackEntry> LayoutGrid(List<FolderStackEntry> entries)
    {
        const int cols = 4;
        const double cell = 72;
        for (var i = 0; i < entries.Count; i++)
        {
            entries[i] = entries[i] with
            {
                GridX = (i % cols) * cell,
                GridY = (i / cols) * cell
            };
        }
        return entries;
    }

    private static string GetKind(string path) =>
        Directory.Exists(path) ? "Folder" : Path.GetExtension(path);
}
