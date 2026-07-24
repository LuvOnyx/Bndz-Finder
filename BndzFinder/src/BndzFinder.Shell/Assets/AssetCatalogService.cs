namespace BndzFinder.Shell.Assets;

public sealed class AssetManifestEntry
{
    public required string Id { get; init; }
    public required string Category { get; init; }
    public required string RelativePath { get; init; }
    public string LicenseStatus { get; init; } = "pending-import";
    public string? SourceUrl { get; init; }
}

public interface IAssetCatalogService
{
    IReadOnlyList<AssetManifestEntry> ListAssets();
    string ResolvePath(string assetId);
    string ResolveAssetsRoot();
    bool IsSystemIconReady(string assetId);
    IReadOnlyList<string> GetMissingSystemIcons();
}

public sealed class AssetCatalogService : IAssetCatalogService
{
    private static readonly AssetManifestEntry[] Catalog =
    [
        new()
        {
            Id = "app-icon-shell",
            Category = "template",
            RelativePath = "templates/apple-design-resources/app-icon-shell.png",
            LicenseStatus = "apple-design-resources",
            SourceUrl = "https://developer.apple.com/design/resources/"
        },
        new()
        {
            Id = "dock-reference",
            Category = "template",
            RelativePath = "templates/apple-design-resources/dock-reference.png",
            LicenseStatus = "apple-design-resources",
            SourceUrl = "https://macosicons.com/resources"
        },
        new() { Id = "icon-finder", Category = "system", RelativePath = "icons/system/finder.png", LicenseStatus = "figma-or-fallback", SourceUrl = "https://www.figma.com/design/60c5Bc1Hr12j9tmVFS5qvW/Bndz-Finder" },
        new() { Id = "icon-trash", Category = "system", RelativePath = "icons/system/trash.png", LicenseStatus = "figma-or-fallback", SourceUrl = "https://www.figma.com/design/60c5Bc1Hr12j9tmVFS5qvW/Bndz-Finder" },
        new() { Id = "icon-launchpad", Category = "system", RelativePath = "icons/system/launchpad.png", LicenseStatus = "figma-or-fallback", SourceUrl = "https://www.figma.com/design/60c5Bc1Hr12j9tmVFS5qvW/Bndz-Finder" },
        new() { Id = "icon-calendar", Category = "system", RelativePath = "icons/system/calendar.png", LicenseStatus = "figma-or-fallback", SourceUrl = "https://www.figma.com/design/60c5Bc1Hr12j9tmVFS5qvW/Bndz-Finder" },
        new() { Id = "icon-weather", Category = "system", RelativePath = "icons/system/weather.png", LicenseStatus = "figma-or-fallback", SourceUrl = "https://www.figma.com/design/60c5Bc1Hr12j9tmVFS5qvW/Bndz-Finder" },
        new() { Id = "icon-preferences", Category = "system", RelativePath = "icons/system/preferences.png", LicenseStatus = "figma-or-fallback", SourceUrl = "https://www.figma.com/design/60c5Bc1Hr12j9tmVFS5qvW/Bndz-Finder" },
        new() { Id = "figma-dock-glass", Category = "figma", RelativePath = "figma/dock/dock-glass.png", LicenseStatus = "figma", SourceUrl = "https://www.figma.com/design/60c5Bc1Hr12j9tmVFS5qvW/Bndz-Finder" },
        new() { Id = "figma-apple-mark", Category = "figma", RelativePath = "figma/menu-bar/apple-mark.png", LicenseStatus = "figma", SourceUrl = "https://www.figma.com/design/60c5Bc1Hr12j9tmVFS5qvW/Bndz-Finder" },
        new() { Id = "shader-liquid-glass", Category = "shader", RelativePath = "shaders/liquid_glass.hlsl" }
    ];

    private static readonly string[] SystemIconIds =
    [
        "icon-finder", "icon-trash", "icon-launchpad",
        "icon-calendar", "icon-weather", "icon-preferences"
    ];

    public IReadOnlyList<AssetManifestEntry> ListAssets() => Catalog;

    public string ResolveAssetsRoot()
    {
        foreach (var root in CandidateRoots())
        {
            if (Directory.Exists(root))
                return root;
        }

        var fallback = CandidateRoots().First();
        Directory.CreateDirectory(fallback);
        return fallback;
    }

    public string ResolvePath(string assetId)
    {
        var entry = Catalog.FirstOrDefault(a => a.Id == assetId);
        if (entry is null) return string.Empty;
        return Path.Combine(ResolveAssetsRoot(), entry.RelativePath);
    }

    public bool IsSystemIconReady(string assetId) =>
        SystemIconIds.Contains(assetId) && File.Exists(ResolvePath(assetId));

    public IReadOnlyList<string> GetMissingSystemIcons() =>
        SystemIconIds.Where(id => !IsSystemIconReady(id)).ToList();

    private static IEnumerable<string> CandidateRoots()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "assets");

        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            foreach (var candidate in new[]
            {
                Path.Combine(dir, "assets"),
                Path.Combine(dir, "BndzFinder", "assets")
            })
            {
                if (Directory.Exists(candidate))
                    yield return Path.GetFullPath(candidate);
            }

            var parent = Directory.GetParent(dir);
            if (parent is null) break;
            dir = parent.FullName;
        }

        yield return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "BndzFinder", "assets"));
        yield return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "assets"));
    }
}
