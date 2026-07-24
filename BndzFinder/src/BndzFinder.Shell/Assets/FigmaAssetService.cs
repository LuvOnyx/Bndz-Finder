using System.Text.Json;

namespace BndzFinder.Shell.Assets;

/// <summary>
/// Discovers and applies Figma-exported assets under assets/figma/.
/// Prefer these over Skia fallbacks whenever files exist.
/// </summary>
public interface IFigmaAssetService
{
    string? ResolveFigmaRoot();
    string? ResolveIcon(string assetId);
    string? ResolveDockGlass();
    string? ResolveAppleMark();
    string? ResolveMenuGlyph(string name);
    FigmaDesignTokens? LoadTokens();
    void SyncSystemIconsFromFigma(IAssetCatalogService catalog);
    IReadOnlyList<string> ListAvailableIconIds();
}

public sealed class FigmaDesignTokens
{
    public FigmaMenuBarTokens? MenuBar { get; set; }
    public FigmaDockTokens? Dock { get; set; }
    public Dictionary<string, string>? IconFileMap { get; set; }
}

public sealed class FigmaMenuBarTokens
{
    public double Height { get; set; } = 24;
    public double FontSize { get; set; } = 13;
    public string? TintDark { get; set; }
    public double TintOpacity { get; set; } = 0.55;
    public string? Foreground { get; set; }
}

public sealed class FigmaDockTokens
{
    public double CornerRadius { get; set; } = 22;
    public double IconSize { get; set; } = 52;
    public double IconMaxSize { get; set; } = 78;
    public double IconSpacing { get; set; } = 6;
    public string? TintDark { get; set; }
    public double TintOpacity { get; set; } = 0.32;
    public double BorderOpacity { get; set; } = 0.24;
    public double HighlightOpacity { get; set; } = 0.28;
}

public sealed class FigmaAssetService : IFigmaAssetService
{
    private static readonly Dictionary<string, string> DefaultIconMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["icon-finder"] = "finder.png",
        ["icon-launchpad"] = "launchpad.png",
        ["icon-safari"] = "safari.png",
        ["icon-messages"] = "messages.png",
        ["icon-mail"] = "mail.png",
        ["icon-maps"] = "maps.png",
        ["icon-photos"] = "photos.png",
        ["icon-facetime"] = "facetime.png",
        ["icon-calendar"] = "calendar.png",
        ["icon-contacts"] = "contacts.png",
        ["icon-notes"] = "notes.png",
        ["icon-apple-tv"] = "apple-tv.png",
        ["icon-music"] = "music.png",
        ["icon-podcasts"] = "podcasts.png",
        ["icon-reminders"] = "reminders.png",
        ["icon-stocks"] = "stocks.png",
        ["icon-app-store"] = "app-store.png",
        ["icon-preferences"] = "settings.png",
        ["icon-news"] = "news.png",
        ["icon-voice-memos"] = "voice-memos.png",
        ["icon-downloads"] = "downloads.png",
        ["icon-trash"] = "trash.png",
        ["icon-weather"] = "weather.png"
    };

    private readonly IAssetCatalogService _catalog;

    public FigmaAssetService(IAssetCatalogService? catalog = null)
    {
        _catalog = catalog ?? new AssetCatalogService();
    }

    public string? ResolveFigmaRoot()
    {
        var root = Path.Combine(_catalog.ResolveAssetsRoot(), "figma");
        return Directory.Exists(root) ? root : null;
    }

    public FigmaDesignTokens? LoadTokens()
    {
        var figma = ResolveFigmaRoot();
        if (figma is null) return null;
        foreach (var name in new[] { "tokens.json", "tokens.example.json" })
        {
            var path = Path.Combine(figma, name);
            if (!File.Exists(path)) continue;
            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<FigmaDesignTokens>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                // ignore malformed tokens
            }
        }
        return null;
    }

    public string? ResolveIcon(string assetId)
    {
        var figma = ResolveFigmaRoot();
        if (figma is null) return null;

        var map = LoadTokens()?.IconFileMap ?? DefaultIconMap;
        if (!map.TryGetValue(assetId, out var file))
        {
            // allow bare id like "finder" → finder.png
            var bare = assetId.StartsWith("icon-", StringComparison.OrdinalIgnoreCase)
                ? assetId["icon-".Length..]
                : assetId;
            file = bare + ".png";
        }

        var path = Path.Combine(figma, "icons", file);
        return File.Exists(path) ? path : null;
    }

    public string? ResolveDockGlass()
    {
        var figma = ResolveFigmaRoot();
        if (figma is null) return null;
        var path = Path.Combine(figma, "dock", "dock-glass.png");
        return File.Exists(path) ? path : null;
    }

    public string? ResolveAppleMark()
    {
        var figma = ResolveFigmaRoot();
        if (figma is null) return null;
        foreach (var name in new[] { "apple-mark.png", "apple.png" })
        {
            var path = Path.Combine(figma, "menu-bar", name);
            if (File.Exists(path)) return path;
        }
        return null;
    }

    public string? ResolveMenuGlyph(string name)
    {
        var figma = ResolveFigmaRoot();
        if (figma is null) return null;
        var path = Path.Combine(figma, "menu-bar", name);
        return File.Exists(path) ? path : null;
    }

    public void SyncSystemIconsFromFigma(IAssetCatalogService catalog)
    {
        foreach (var id in new[]
                 {
                     "icon-finder", "icon-launchpad", "icon-calendar",
                     "icon-trash", "icon-weather", "icon-preferences"
                 })
        {
            var figmaIcon = ResolveIcon(id);
            if (figmaIcon is null) continue;
            var dest = catalog.ResolvePath(id);
            if (string.IsNullOrWhiteSpace(dest)) continue;
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(figmaIcon, dest, overwrite: true);
        }

        var glass = ResolveDockGlass();
        if (glass is not null)
        {
            var dockRef = catalog.ResolvePath("dock-reference");
            if (!string.IsNullOrWhiteSpace(dockRef))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dockRef)!);
                File.Copy(glass, dockRef, overwrite: true);
            }
        }
    }

    public IReadOnlyList<string> ListAvailableIconIds()
    {
        var figma = ResolveFigmaRoot();
        if (figma is null) return [];
        var iconsDir = Path.Combine(figma, "icons");
        if (!Directory.Exists(iconsDir)) return [];

        var map = LoadTokens()?.IconFileMap ?? DefaultIconMap;
        return map
            .Where(kv => File.Exists(Path.Combine(iconsDir, kv.Value)))
            .Select(kv => kv.Key)
            .ToList();
    }
}
