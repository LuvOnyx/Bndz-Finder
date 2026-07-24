using BndzFinder.Core.Settings;
using BndzFinder.Shell.Assets;

namespace BndzFinder.Shell.Services;

/// <summary>
/// Ensures macOS-style branding assets exist at runtime.
/// Order: Figma exports → Apple Design Resources → Skia fallbacks.
/// </summary>
public interface IMacBrandingBootstrap
{
    void EnsureBrandingAssets();
    void ApplyFigmaTokens(BndzFinderSettings settings);
}

public sealed class MacBrandingBootstrap : IMacBrandingBootstrap
{
    private readonly IAssetCatalogService _catalog;
    private readonly IFigmaAssetService _figma;
    private readonly string _themesDirectory;

    public MacBrandingBootstrap(
        IAssetCatalogService? catalog = null,
        IFigmaAssetService? figma = null,
        string? themesDirectory = null)
    {
        _catalog = catalog ?? new AssetCatalogService();
        _figma = figma ?? new FigmaAssetService(_catalog);
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _themesDirectory = themesDirectory ?? Path.Combine(appData, "BndzFinder", "themes");
    }

    public void EnsureBrandingAssets()
    {
        // Prefer Figma PNGs when present (sync into icons/system + dock reference).
        _figma.SyncSystemIconsFromFigma(_catalog);

        // Fill any remaining gaps with Skia squircles.
        SystemIconFallbackGenerator.EnsureAllSystemIcons(_catalog);

        BundledAssetGenerator.EnsureDefaultThemePack(_themesDirectory);
        BundledAssetGenerator.EnsureAppIconShellTemplate(
            _catalog.ResolvePath("app-icon-shell"));

        // Overlay Figma dock glass + wallpapers into the default theme pack when available.
        OverlayFigmaThemePack();
    }

    public void ApplyFigmaTokens(BndzFinderSettings settings)
    {
        var tokens = _figma.LoadTokens();
        if (tokens is null) return;

        if (tokens.MenuBar is { } menu)
        {
            if (menu.Height > 0)
                settings.FinderHeight = (int)Math.Round(menu.Height);
        }

        if (tokens.Dock is { } dock)
        {
            if (dock.CornerRadius > 0)
                settings.DockCornerRadius = dock.CornerRadius;
            if (dock.IconSize > 0)
                settings.IconSize = (int)Math.Round(dock.IconSize);
            if (dock.IconMaxSize > 0)
                settings.IconMaxSize = (int)Math.Round(dock.IconMaxSize);
            if (dock.IconSpacing > 0)
                settings.IconSpace = (int)Math.Round(dock.IconSpacing);
        }
    }

    private void OverlayFigmaThemePack()
    {
        var themeDir = Path.Combine(_themesDirectory, BundledAssetGenerator.DefaultThemeId);
        Directory.CreateDirectory(themeDir);

        var glass = _figma.ResolveDockGlass();
        if (glass is not null)
            File.Copy(glass, Path.Combine(themeDir, "dock-glass.png"), overwrite: true);

        var figmaRoot = _figma.ResolveFigmaRoot();
        if (figmaRoot is null) return;

        var wallDir = Path.Combine(figmaRoot, "wallpapers");
        if (!Directory.Exists(wallDir)) return;

        foreach (var file in Directory.EnumerateFiles(wallDir))
        {
            var name = Path.GetFileName(file);
            File.Copy(file, Path.Combine(themeDir, name), overwrite: true);
        }
    }
}
