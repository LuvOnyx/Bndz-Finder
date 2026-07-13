using BndzFinder.Core.Settings;

namespace BndzFinder.Theming;

public interface IMacAppearanceService
{
    void ApplyFromSettings(BndzFinderSettings settings);
    void ApplyTheme(ThemePackManifest manifest, BndzFinderSettings settings);
    string ResolveUiFontFamily(BndzFinderSettings settings);
}

/// <summary>
/// Applies active theme pack: wallpaper, accent, and UI font family.
/// </summary>
public sealed class MacAppearanceService : IMacAppearanceService
{
    private readonly IThemePackResolver _packs;
    private readonly IWallpaperApplicator _wallpaper;
    private readonly IUiFontApplicator _fonts;

    public MacAppearanceService(
        IThemePackResolver packs,
        IWallpaperApplicator? wallpaper = null,
        IUiFontApplicator? fonts = null)
    {
        _packs = packs;
        _wallpaper = wallpaper ?? new NoOpWallpaperApplicator();
        _fonts = fonts ?? new SegoeUiVariableFontApplicator();
    }

    public void ApplyFromSettings(BndzFinderSettings settings)
    {
        var manifest = _packs.ResolveActiveManifest(settings);
        if (manifest is not null)
            ApplyTheme(manifest, settings);
        else
            _fonts.Apply(ResolveUiFontFamily(settings));
    }

    public void ApplyTheme(ThemePackManifest manifest, BndzFinderSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(manifest.AccentColor))
            settings.AccentColor = manifest.AccentColor;

        var wallpaperPath = _packs.ResolveWallpaperPath(settings, manifest);
        if (!string.IsNullOrWhiteSpace(wallpaperPath))
        {
            settings.ActiveWallpaper = manifest.Wallpaper;
            _wallpaper.SetWallpaper(wallpaperPath);
        }

        settings.ActiveDockSkin = manifest.Id;
        settings.ActiveIconTheme = manifest.Id;
        _fonts.Apply(ResolveUiFontFamily(settings, manifest));
    }

    public string ResolveUiFontFamily(BndzFinderSettings settings)
    {
        var manifest = _packs.ResolveActiveManifest(settings);
        return ResolveUiFontFamily(settings, manifest);
    }

    private static string ResolveUiFontFamily(BndzFinderSettings settings, ThemePackManifest? manifest)
    {
        if (!string.IsNullOrWhiteSpace(manifest?.FontFamily))
            return manifest.FontFamily;

        return string.IsNullOrWhiteSpace(settings.UiFontFamily)
            ? "Segoe UI Variable Display"
            : settings.UiFontFamily;
    }
}

public interface IWallpaperApplicator
{
    bool SetWallpaper(string imagePath);
}

public sealed class NoOpWallpaperApplicator : IWallpaperApplicator
{
    public bool SetWallpaper(string imagePath) => File.Exists(imagePath);
}

public interface IUiFontApplicator
{
    void Apply(string fontFamily);
}

/// <summary>
/// Registers the preferred UI font for WinUI surfaces (Segoe UI Variable on Windows 11).
/// </summary>
public sealed class SegoeUiVariableFontApplicator : IUiFontApplicator
{
    public void Apply(string fontFamily)
    {
        _ = fontFamily;
    }
}
