namespace BndzFinder.Shell.Assets;

public sealed class AssetManifestEntry
{
    public required string Id { get; init; }
    public required string Category { get; init; }
    public required string RelativePath { get; init; }
    public string LicenseStatus { get; init; } = "design-reference";
}

public interface IAssetCatalogService
{
    IReadOnlyList<AssetManifestEntry> ListAssets();
    string ResolvePath(string assetId);
    bool TryImportPlaceholder(string assetId, string targetDirectory);
}

public sealed class AssetCatalogService : IAssetCatalogService
{
    private static readonly AssetManifestEntry[] Catalog =
    [
        new() { Id = "dock-shell-template", Category = "template", RelativePath = "templates/dock-icon-shell.svg" },
        new() { Id = "icon-finder", Category = "system", RelativePath = "icons/system/finder.svg" },
        new() { Id = "icon-trash", Category = "system", RelativePath = "icons/system/trash.svg" },
        new() { Id = "icon-launchpad", Category = "system", RelativePath = "icons/system/launchpad.svg" },
        new() { Id = "icon-calendar", Category = "system", RelativePath = "icons/system/calendar.svg" },
        new() { Id = "icon-weather", Category = "system", RelativePath = "icons/system/weather.svg" },
        new() { Id = "icon-preferences", Category = "system", RelativePath = "icons/system/preferences.svg" },
        new() { Id = "sound-minimize", Category = "audio", RelativePath = "sounds/minimize.wav" },
        new() { Id = "shader-liquid-glass", Category = "shader", RelativePath = "shaders/liquid_glass.hlsl" }
    ];

    public IReadOnlyList<AssetManifestEntry> ListAssets() => Catalog;

    public string ResolvePath(string assetId)
    {
        var entry = Catalog.FirstOrDefault(a => a.Id == assetId);
        if (entry is null) return string.Empty;
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "assets"));
        if (!Directory.Exists(root))
            root = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "BndzFinder", "assets"));
        return Path.Combine(root, entry.RelativePath);
    }

    public bool TryImportPlaceholder(string assetId, string targetDirectory)
    {
        var source = ResolvePath(assetId);
        if (!File.Exists(source)) return false;
        Directory.CreateDirectory(targetDirectory);
        var dest = Path.Combine(targetDirectory, Path.GetFileName(source));
        File.Copy(source, dest, overwrite: true);
        return true;
    }
}
