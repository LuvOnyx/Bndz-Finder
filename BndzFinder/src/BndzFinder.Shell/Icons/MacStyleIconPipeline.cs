using BndzFinder.Core.Design;
using BndzFinder.Shell.Assets;
using SkiaSharp;

namespace BndzFinder.Shell.Icons;

public interface IIconPipeline
{
    Task<IconPipelineResult> ProcessAsync(string targetPath, CancellationToken cancellationToken = default);
    string GetCachePath(string targetPath);
}

public sealed class IconPipelineResult
{
    public required string CacheFilePath { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
}

public sealed class MacStyleIconPipeline : IIconPipeline
{
    private readonly string _cacheDirectory;
    private readonly string? _appIconShellPath;

    public MacStyleIconPipeline(string? cacheDirectory = null, string? appIconShellPath = null)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _cacheDirectory = cacheDirectory ?? Path.Combine(appData, "BndzFinder", "IconCache");
        Directory.CreateDirectory(_cacheDirectory);
        _appIconShellPath = appIconShellPath ?? ResolveShellOverlayPath();
    }

    public string GetCachePath(string targetPath)
    {
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(targetPath.ToLowerInvariant())));
        return Path.Combine(_cacheDirectory, $"{hash}.png");
    }

    public async Task<IconPipelineResult> ProcessAsync(string targetPath, CancellationToken cancellationToken = default)
    {
        if (IsImportedMacOsIcon(targetPath) && File.Exists(targetPath))
        {
            return new IconPipelineResult
            {
                CacheFilePath = targetPath,
                Width = AppleDesignMetrics.IconCanvasSize,
                Height = AppleDesignMetrics.IconCanvasSize
            };
        }

        var cachePath = GetCachePath(targetPath);
        if (File.Exists(cachePath))
        {
            return new IconPipelineResult
            {
                CacheFilePath = cachePath,
                Width = AppleDesignMetrics.IconCanvasSize,
                Height = AppleDesignMetrics.IconCanvasSize
            };
        }

        await Task.Run(() =>
        {
            var canvasSize = AppleDesignMetrics.IconCanvasSize;
            using var surface = SKSurface.Create(new SKImageInfo(canvasSize, canvasSize, SKColorType.Rgba8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            var padding = AppleDesignMetrics.IconContentInset;
            var contentRect = new SKRect(padding, padding, padding + AppleDesignMetrics.IconContentSize, padding + AppleDesignMetrics.IconContentSize);
            var cornerRadius = AppleDesignMetrics.IconContentSize * AppleDesignMetrics.IconSquircleRadiusRatio;

            using var clipPath = AppleIconShellRenderer.CreateSquirclePath(contentRect, cornerRadius);
            canvas.Save();
            canvas.ClipPath(clipPath, SKClipOperation.Intersect, true);
            DrawSourceIcon(canvas, targetPath, contentRect, cornerRadius);
            canvas.Restore();

            AppleIconShellRenderer.ApplyShell(canvas, contentRect, cornerRadius, _appIconShellPath);

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.OpenWrite(cachePath);
            data.SaveTo(stream);
        }, cancellationToken).ConfigureAwait(false);

        return new IconPipelineResult
        {
            CacheFilePath = cachePath,
            Width = AppleDesignMetrics.IconCanvasSize,
            Height = AppleDesignMetrics.IconCanvasSize
        };
    }

    private static bool IsImportedMacOsIcon(string targetPath)
    {
        var normalized = targetPath.Replace('\\', '/');
        return normalized.Contains("/icons/system/", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ResolveShellOverlayPath()
    {
        var catalog = new AssetCatalogService();
        var path = catalog.ResolvePath("app-icon-shell");
        return File.Exists(path) ? path : null;
    }

    private static void DrawSourceIcon(SKCanvas canvas, string targetPath, SKRect rect, float cornerRadius)
    {
        var sourcePath = ResolveSourceIconPath(targetPath);
        if (!string.IsNullOrWhiteSpace(sourcePath) && File.Exists(sourcePath))
        {
            using var bitmap = SKBitmap.Decode(sourcePath);
            if (bitmap is not null)
            {
                canvas.DrawBitmap(bitmap, rect);
                return;
            }
        }

        using var paint = new SKPaint
        {
            Color = new SKColor(0x3A, 0x3A, 0x3C),
            IsAntialias = true
        };
        canvas.DrawRoundRect(rect, cornerRadius, cornerRadius, paint);

        var label = Path.GetFileNameWithoutExtension(targetPath);
        if (string.IsNullOrWhiteSpace(label)) label = "?";
        label = label.Length > 2 ? label[..2].ToUpperInvariant() : label.ToUpperInvariant();

        using var textPaint = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            TextSize = rect.Width * 0.38f,
            Typeface = SKTypeface.FromFamilyName("Segoe UI Variable Display", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright),
            FakeBoldText = true
        };
        canvas.DrawText(label, rect.MidX, rect.MidY + textPaint.TextSize * 0.35f, textPaint);
    }

    private static string? ResolveSourceIconPath(string targetPath)
    {
        if (string.IsNullOrWhiteSpace(targetPath)) return null;
        if (File.Exists(targetPath) &&
            (targetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
             || targetPath.EndsWith(".ico", StringComparison.OrdinalIgnoreCase)))
            return targetPath;

        if (!OperatingSystem.IsWindows()) return null;

        var temp = Path.Combine(Path.GetTempPath(), "BndzFinder", "icon-src",
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(targetPath.ToLowerInvariant()))) + ".png");
        return WindowsFileIconExtractor.TryExtractToPng(targetPath, temp) ? temp : null;
    }
}
