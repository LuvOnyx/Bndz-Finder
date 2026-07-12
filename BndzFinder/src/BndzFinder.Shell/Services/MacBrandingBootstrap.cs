using BndzFinder.Shell.Assets;

namespace BndzFinder.Shell.Services;

/// <summary>
/// Ensures macOS-style branding assets exist at runtime (system icons, app icon shell).
/// Official PNGs from macosicons.com take precedence when imported.
/// </summary>
public interface IMacBrandingBootstrap
{
    void EnsureBrandingAssets();
}

public sealed class MacBrandingBootstrap : IMacBrandingBootstrap
{
    private readonly IAssetCatalogService _catalog;

    public MacBrandingBootstrap(IAssetCatalogService? catalog = null)
    {
        _catalog = catalog ?? new AssetCatalogService();
    }

    public void EnsureBrandingAssets()
    {
        SystemIconFallbackGenerator.EnsureAllSystemIcons(_catalog);
    }
}
