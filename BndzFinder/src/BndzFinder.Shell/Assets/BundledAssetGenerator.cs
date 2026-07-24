using BndzFinder.Core.Design;
using SkiaSharp;

namespace BndzFinder.Shell.Assets;

/// <summary>
/// Generates macOS-style bundled wallpapers, dock skins, and icon shells without external API keys.
/// </summary>
public static class BundledAssetGenerator
{
    public const string DefaultThemeId = "sequoia-default";

    public static void EnsureDefaultThemePack(string themesRoot)
    {
        var themeDir = Path.Combine(themesRoot, DefaultThemeId);
        Directory.CreateDirectory(themeDir);

        EnsureWallpaper(Path.Combine(themeDir, "wallpaper-sequoia.jpg"),
            top: new SKColor(0x1A, 0x3A, 0x5C), bottom: new SKColor(0x4A, 0x7C, 0x9B), accent: new SKColor(0xFF, 0x9F, 0x43));
        EnsureWallpaper(Path.Combine(themeDir, "wallpaper-sonoma.jpg"),
            top: new SKColor(0x2D, 0x1B, 0x4E), bottom: new SKColor(0x8B, 0x5C, 0xF6), accent: new SKColor(0xF4, 0x72, 0xB6));
        EnsureWallpaper(Path.Combine(themeDir, "wallpaper-tahoe-night.jpg"),
            top: new SKColor(0x0B, 0x0D, 0x17), bottom: new SKColor(0x1E, 0x29, 0x3B), accent: new SKColor(0x38, 0xBD, 0xF8));

        EnsureDockSkin(Path.Combine(themeDir, "dock-glass.png"));
        EnsureIconShell(Path.Combine(themeDir, "icon-shell.png"));

        var manifestPath = Path.Combine(themeDir, "manifest.json");
        if (!File.Exists(manifestPath))
        {
            File.WriteAllText(manifestPath, """
                {
                  "id": "sequoia-default",
                  "name": "macOS Sequoia",
                  "author": "Bndz-Finder",
                  "version": "1.0.0",
                  "dockSkin": "dock-glass.png",
                  "iconShell": "icon-shell.png",
                  "wallpaper": "wallpaper-sequoia.jpg",
                  "accentColor": "#0A84FF",
                  "fontFamily": "Segoe UI Variable Display"
                }
                """);
        }
    }

    public static void EnsureAppIconShellTemplate(string targetPath)
    {
        if (File.Exists(targetPath)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath) ?? ".");
        EnsureIconShell(targetPath);
    }

    private static void EnsureWallpaper(string path, SKColor top, SKColor bottom, SKColor accent)
    {
        if (File.Exists(path)) return;

        const int width = 3840;
        const int height = 2160;
        using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;

        using var sky = new SKPaint
        {
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0),
                new SKPoint(0, height),
                [top, bottom],
                SKShaderTileMode.Clamp),
            IsAntialias = true
        };
        canvas.DrawRect(0, 0, width, height, sky);

        using var glow = new SKPaint
        {
            Shader = SKShader.CreateRadialGradient(
                new SKPoint(width * 0.72f, height * 0.35f),
                width * 0.45f,
                [accent.WithAlpha(180), accent.WithAlpha(0)],
                SKShaderTileMode.Clamp),
            IsAntialias = true
        };
        canvas.DrawCircle(width * 0.72f, height * 0.35f, width * 0.45f, glow);

        using var haze = new SKPaint
        {
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(0, height * 0.55f),
                new SKPoint(0, height),
                [SKColors.Transparent, new SKColor(255, 255, 255, 30)],
                SKShaderTileMode.Clamp),
            IsAntialias = true
        };
        canvas.DrawRect(0, height * 0.55f, width, height * 0.45f, haze);

        SaveJpeg(surface, path, 92);
    }

    private static void EnsureDockSkin(string path)
    {
        if (File.Exists(path)) return;

        const int width = 1920;
        const int height = 128;
        using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        using var glass = new SKPaint
        {
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0),
                new SKPoint(0, height),
                [new SKColor(0x26, 0x26, 0x26, 140), new SKColor(0x26, 0x26, 0x26, 70)],
                SKShaderTileMode.Clamp),
            IsAntialias = true
        };
        canvas.DrawRoundRect(new SKRect(0, 8, width, height - 8), 44, 44, glass);

        using var edge = new SKPaint
        {
            Color = new SKColor(255, 255, 255, 48),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.5f,
            IsAntialias = true
        };
        canvas.DrawRoundRect(new SKRect(1, 9, width - 1, height - 9), 44, 44, edge);

        using var highlight = new SKPaint
        {
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 8),
                new SKPoint(0, 28),
                [new SKColor(255, 255, 255, 70), new SKColor(255, 255, 255, 0)],
                SKShaderTileMode.Clamp),
            IsAntialias = true
        };
        canvas.DrawRoundRect(new SKRect(2, 10, width - 2, 28), 40, 40, highlight);

        SavePng(surface, path);
    }

    private static void EnsureIconShell(string path)
    {
        if (File.Exists(path)) return;

        var size = AppleDesignMetrics.IconCanvasSize;
        using var surface = SKSurface.Create(new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var padding = AppleDesignMetrics.IconContentInset;
        var contentRect = new SKRect(padding, padding, padding + AppleDesignMetrics.IconContentSize, padding + AppleDesignMetrics.IconContentSize);
        var cornerRadius = AppleDesignMetrics.IconContentSize * AppleDesignMetrics.IconSquircleRadiusRatio;

        using var highlight = new SKPaint
        {
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(contentRect.Left, contentRect.Top),
                new SKPoint(contentRect.Left, contentRect.Top + contentRect.Height * 0.45f),
                [new SKColor(255, 255, 255, 90), new SKColor(255, 255, 255, 0)],
                SKShaderTileMode.Clamp),
            IsAntialias = true
        };
        canvas.DrawRoundRect(contentRect, cornerRadius, cornerRadius, highlight);

        using var rim = new SKPaint
        {
            Color = new SKColor(255, 255, 255, 28),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 3,
            IsAntialias = true
        };
        canvas.DrawRoundRect(contentRect, cornerRadius, cornerRadius, rim);

        SavePng(surface, path);
    }

    private static void SavePng(SKSurface surface, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(path);
        data.SaveTo(stream);
    }

    private static void SaveJpeg(SKSurface surface, string path, int quality)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);
        using var stream = File.OpenWrite(path);
        data.SaveTo(stream);
    }
}
