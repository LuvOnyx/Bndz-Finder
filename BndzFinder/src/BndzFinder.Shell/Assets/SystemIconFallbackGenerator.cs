using SkiaSharp;

namespace BndzFinder.Shell.Assets;

/// <summary>
/// Generates branded fallback PNGs when macosicons system assets are not imported.
/// </summary>
public static class SystemIconFallbackGenerator
{
    private static readonly Dictionary<string, (string Glyph, SKColor Color)> Profiles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["icon-finder"] = ("F", new SKColor(0x0A, 0x84, 0xFF)),
        ["icon-launchpad"] = ("▦", new SKColor(0x5E, 0x5C, 0xE6)),
        ["icon-calendar"] = ("📅", new SKColor(0xFF, 0x45, 0x3A)),
        ["icon-trash"] = ("🗑", new SKColor(0x8E, 0x8E, 0x93)),
        ["icon-weather"] = ("☀", new SKColor(0xFF, 0xCC, 0x00)),
        ["icon-preferences"] = ("⚙", new SKColor(0xAE, 0xAE, 0xB2))
    };

    public static string EnsureFallback(string assetId, string targetPath)
    {
        if (File.Exists(targetPath)) return targetPath;
        if (!Profiles.TryGetValue(assetId, out var profile)) return targetPath;

        Directory.CreateDirectory(Path.GetDirectoryName(targetPath) ?? ".");
        const int size = 256;
        using var surface = SKSurface.Create(new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var rect = new SKRect(24, 24, size - 24, size - 24);
        using var bg = new SKPaint { Color = profile.Color, IsAntialias = true };
        canvas.DrawRoundRect(rect, 48, 48, bg);

        using var text = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            TextSize = 96,
            Typeface = SKTypeface.FromFamilyName("Segoe UI Symbol")
        };
        canvas.DrawText(profile.Glyph, size / 2f, size / 2f + 32, text);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(targetPath);
        data.SaveTo(stream);
        return targetPath;
    }
}
