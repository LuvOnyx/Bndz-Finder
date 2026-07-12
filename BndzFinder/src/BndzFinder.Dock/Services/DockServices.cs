using BndzFinder.Core.Models;

namespace BndzFinder.Dock.Services;

public interface IDockCompositor
{
    void RenderGlassBackdrop(GlassEffect effect, double blurIntensity);
    void RenderIcons(IReadOnlyList<Shell.Dock.DockLayoutItem> items);
}

public sealed class DockCompositor : IDockCompositor
{
    public void RenderGlassBackdrop(GlassEffect effect, double blurIntensity)
    {
        _ = effect;
        _ = blurIntensity;
    }

    public void RenderIcons(IReadOnlyList<Shell.Dock.DockLayoutItem> items)
    {
        _ = items;
    }
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
            _ => files.ToList()
        };
    }
}

public sealed class WeatherDockService
{
    public string GetCurrentCondition() => "Clear";
    public IReadOnlyList<string> GetForecast() => ["Clear", "Partly Cloudy", "Rain"];
}

public sealed class ProgressBarMirrorService
{
    public double GetProgress(string appPath) => 0;
}
