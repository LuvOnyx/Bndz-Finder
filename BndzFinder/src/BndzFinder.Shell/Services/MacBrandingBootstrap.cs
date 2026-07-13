using BndzFinder.Shell.Assets;

namespace BndzFinder.Shell.Services;

/// <summary>
/// Ensures macOS-style branding assets exist at runtime (system icons, themes, wallpapers, icon shell).
/// Official PNGs from Apple Design Resources or macosicons.com take precedence when imported.
/// </summary>
public interface IMacBrandingBootstrap
{
    void EnsureBrandingAssets();
}

public sealed class MacBrandingBootstrap : IMacBrandingBootstrap
{
    private readonly IAssetCatalogService _catalog;
    private readonly string _themesDirectory;

    public MacBrandingBootstrap(IAssetCatalogService? catalog = null, string? themesDirectory = null)
    {
        _catalog = catalog ?? new AssetCatalogService();
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _themesDirectory = themesDirectory ?? Path.Combine(appData, "BndzFinder", "themes");
    }

    public void EnsureBrandingAssets()
    {
        SystemIconFallbackGenerator.EnsureAllSystemIcons(_catalog);
        BundledAssetGenerator.EnsureDefaultThemePack(_themesDirectory);
        BundledAssetGenerator.EnsureAppIconShellTemplate(
            _catalog.ResolvePath("app-icon-shell"));
    }
}
