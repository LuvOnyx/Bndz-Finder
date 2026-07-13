using BndzFinder.Interop;
using BndzFinder.Theming;

namespace BndzFinder.App;

public sealed class WindowsWallpaperApplicator : IWallpaperApplicator
{
    private readonly IWallpaperService _service;

    public WindowsWallpaperApplicator(IWallpaperService? service = null)
    {
        _service = service ?? new WindowsWallpaperService();
    }

    public bool SetWallpaper(string imagePath) => _service.SetWallpaper(imagePath);
}

public sealed class WinUiFontApplicator : IUiFontApplicator
{
    public void Apply(string fontFamily)
    {
        if (string.IsNullOrWhiteSpace(fontFamily)) return;

        var family = new Microsoft.UI.Xaml.Media.FontFamily(fontFamily);
        if (Application.Current?.Resources is null) return;

        Application.Current.Resources["ContentControlThemeFontFamily"] = family;
        Application.Current.Resources["TextControlThemeFontFamily"] = family;
    }
}
