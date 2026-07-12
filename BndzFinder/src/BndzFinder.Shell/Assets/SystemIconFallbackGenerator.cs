using BndzFinder.Core.Design;
using BndzFinder.Shell.Icons;
using SkiaSharp;

namespace BndzFinder.Shell.Assets;

/// <summary>
/// Generates macOS-style squircle system icons when official macosicons PNGs are not imported.
/// Uses Apple Design Resources proportions (1024 canvas, squircle gloss shell).
/// </summary>
public static class SystemIconFallbackGenerator
{
    private sealed record IconProfile(SKColor Base, SKColor Accent, string Label);

    private static readonly Dictionary<string, IconProfile> Profiles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["icon-finder"] = new(new SKColor(0x0A, 0x84, 0xFF), new SKColor(0x5A, 0xC8, 0xFA), "Finder"),
        ["icon-launchpad"] = new(new SKColor(0x5E, 0x5C, 0xE6), new SKColor(0xBF, 0x5A, 0xF2), "Launchpad"),
        ["icon-calendar"] = new(new SKColor(0xFF, 0x3B, 0x30), new SKColor(0xFF, 0x95, 0x8A), "Calendar"),
        ["icon-trash"] = new(new SKColor(0x8E, 0x8E, 0x93), new SKColor(0xC7, 0xC7, 0xCC), "Trash"),
        ["icon-weather"] = new(new SKColor(0x34, 0xC7, 0x59), new SKColor(0x5A, 0xC8, 0xFA), "Weather"),
        ["icon-preferences"] = new(new SKColor(0x8E, 0x8E, 0x93), new SKColor(0xAE, 0xAE, 0xB2), "Settings")
    };

    public static string EnsureFallback(string assetId, string targetPath)
    {
        if (File.Exists(targetPath)) return targetPath;
        if (!Profiles.TryGetValue(assetId, out var profile)) return targetPath;

        Directory.CreateDirectory(Path.GetDirectoryName(targetPath) ?? ".");
        var size = AppleDesignMetrics.IconCanvasSize;
        using var surface = SKSurface.Create(new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var padding = AppleDesignMetrics.IconContentInset;
        var contentRect = new SKRect(padding, padding, padding + AppleDesignMetrics.IconContentSize, padding + AppleDesignMetrics.IconContentSize);
        var cornerRadius = AppleDesignMetrics.IconContentSize * AppleDesignMetrics.IconSquircleRadiusRatio;

        using var clipPath = AppleIconShellRenderer.CreateSquirclePath(contentRect, cornerRadius);
        canvas.Save();
        canvas.ClipPath(clipPath, SKClipOperation.Intersect, true);

        using var gradient = new SKPaint
        {
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(contentRect.Left, contentRect.Top),
                new SKPoint(contentRect.Right, contentRect.Bottom),
                [profile.Base, profile.Accent],
                SKShaderTileMode.Clamp),
            IsAntialias = true
        };
        canvas.DrawRoundRect(contentRect, cornerRadius, cornerRadius, gradient);
        canvas.Restore();

        AppleIconShellRenderer.ApplyShell(canvas, contentRect, cornerRadius, shellOverlayPath: null);

        using var labelPaint = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            TextSize = 140,
            Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright),
            FakeBoldText = true
        };
        canvas.DrawText(profile.Label, size / 2f, size / 2f + 48, labelPaint);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(targetPath);
        data.SaveTo(stream);
        return targetPath;
    }

    public static void EnsureAllSystemIcons(IAssetCatalogService catalog)
    {
        foreach (var id in new[] { "icon-finder", "icon-launchpad", "icon-calendar", "icon-trash", "icon-weather", "icon-preferences" })
            EnsureFallback(id, catalog.ResolvePath(id));
    }
}
