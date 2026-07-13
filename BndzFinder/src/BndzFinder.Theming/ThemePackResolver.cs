using BndzFinder.Core.Settings;
using System.Text.Json;

namespace BndzFinder.Theming;

public interface IThemePackResolver
{
    string? ResolvePackDirectory(string? packId);
    string? ResolveDockSkinPath(BndzFinderSettings settings);
    string? ResolveIconShellPath(BndzFinderSettings settings);
    string? ResolveTimeSkinPath(BndzFinderSettings settings);
    string? ResolveWallpaperPath(BndzFinderSettings settings, ThemePackManifest? manifest = null);
    IReadOnlyList<string> ListWallpapers(BndzFinderSettings settings);
    ThemePackManifest? ResolveActiveManifest(BndzFinderSettings settings);
}

public sealed class ThemePackResolver : IThemePackResolver
{
    private readonly IThemePackService _packs;
    private readonly string _themesDirectory;

    public ThemePackResolver(IThemePackService? packs = null, string? themesDirectory = null)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _themesDirectory = themesDirectory ?? Path.Combine(appData, "BndzFinder", "themes");
        _packs = packs ?? new ThemePackService(_themesDirectory);
    }

    public string? ResolvePackDirectory(string? packId)
    {
        if (string.IsNullOrWhiteSpace(packId)) return null;
        var dir = Path.Combine(_themesDirectory, packId);
        return Directory.Exists(dir) ? dir : null;
    }

    public ThemePackManifest? ResolveActiveManifest(BndzFinderSettings settings)
    {
        var id = settings.ActiveDockSkin ?? settings.ActiveIconTheme;
        if (string.IsNullOrWhiteSpace(id)) return _packs.ListInstalled().FirstOrDefault();

        return _packs.ListInstalled().FirstOrDefault(p =>
                   p.Id.Equals(id, StringComparison.OrdinalIgnoreCase)
                   || p.Name.Equals(id, StringComparison.OrdinalIgnoreCase))
               ?? _packs.ListInstalled().FirstOrDefault();
    }

    public string? ResolveDockSkinPath(BndzFinderSettings settings)
    {
        var manifest = ResolveActiveManifest(settings);
        if (manifest?.DockSkin is null) return null;
        var dir = ResolvePackDirectory(manifest.Id);
        if (dir is null) return null;
        var path = Path.Combine(dir, manifest.DockSkin);
        return File.Exists(path) ? path : null;
    }

    public string? ResolveIconShellPath(BndzFinderSettings settings)
    {
        var manifest = ResolveActiveManifest(settings);
        if (manifest?.IconShell is null) return null;
        var dir = ResolvePackDirectory(manifest.Id);
        if (dir is null) return null;
        var path = Path.Combine(dir, manifest.IconShell);
        return File.Exists(path) ? path : null;
    }

    public string? ResolveTimeSkinPath(BndzFinderSettings settings)
    {
        var manifest = ResolveActiveManifest(settings);
        if (manifest?.TimeSkin is null) return null;
        var dir = ResolvePackDirectory(manifest.Id);
        if (dir is null) return null;
        var path = Path.Combine(dir, manifest.TimeSkin);
        return File.Exists(path) ? path : null;
    }

    public string? ResolveWallpaperPath(BndzFinderSettings settings, ThemePackManifest? manifest = null)
    {
        manifest ??= ResolveActiveManifest(settings);
        if (manifest is null) return null;

        var dir = ResolvePackDirectory(manifest.Id);
        if (dir is null) return null;

        var wallpaperFile = !string.IsNullOrWhiteSpace(settings.ActiveWallpaper)
            ? settings.ActiveWallpaper
            : manifest.Wallpaper;
        if (string.IsNullOrWhiteSpace(wallpaperFile)) return null;

        var path = Path.Combine(dir, wallpaperFile);
        return File.Exists(path) ? path : null;
    }

    public IReadOnlyList<string> ListWallpapers(BndzFinderSettings settings)
    {
        var manifest = ResolveActiveManifest(settings);
        if (manifest is null) return [];

        var dir = ResolvePackDirectory(manifest.Id);
        if (dir is null) return [];

        return Directory.EnumerateFiles(dir)
            .Where(f =>
            {
                var ext = Path.GetExtension(f);
                return ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                       || ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
                       || ext.Equals(".png", StringComparison.OrdinalIgnoreCase)
                       || ext.Equals(".bmp", StringComparison.OrdinalIgnoreCase);
            })
            .Select(Path.GetFileName)
            .Where(f => f is not null)
            .Cast<string>()
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
