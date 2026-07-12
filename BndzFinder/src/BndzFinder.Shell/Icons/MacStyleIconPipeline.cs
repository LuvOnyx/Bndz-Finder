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
    private const int CanvasSize = 1024;
    private const float ContentSize = 824f;
    private const float CornerRadiusRatio = 0.2237f;
    private readonly string _cacheDirectory;
    private readonly string? _appIconShellPath;

    public MacStyleIconPipeline(string? cacheDirectory = null, string? appIconShellPath = null)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _cacheDirectory = cacheDirectory ?? Path.Combine(appData, "BndzFinder", "IconCache");
        Directory.CreateDirectory(_cacheDirectory);
        _appIconShellPath = appIconShellPath ?? ResolveDefaultShellPath();
    }

    public string GetCachePath(string targetPath)
    {
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(targetPath.ToLowerInvariant())));
        return Path.Combine(_cacheDirectory, $"{hash}.png");
    }

    public async Task<IconPipelineResult> ProcessAsync(string targetPath, CancellationToken cancellationToken = default)
    {
        if (IsMacOsIconsAsset(targetPath) && File.Exists(targetPath))
        {
            return new IconPipelineResult
            {
                CacheFilePath = targetPath,
                Width = CanvasSize,
                Height = CanvasSize
            };
        }

        var cachePath = GetCachePath(targetPath);
        if (File.Exists(cachePath))
        {
            return new IconPipelineResult { CacheFilePath = cachePath, Width = CanvasSize, Height = CanvasSize };
        }

        await Task.Run(() =>
        {
            using var surface = SKSurface.Create(new SKImageInfo(CanvasSize, CanvasSize, SKColorType.Rgba8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            var padding = (CanvasSize - ContentSize) / 2f;
            var contentRect = new SKRect(padding, padding, padding + ContentSize, padding + ContentSize);
            var cornerRadius = ContentSize * CornerRadiusRatio;

            using var clipPath = new SKPath();
            clipPath.AddRoundRect(contentRect, cornerRadius, cornerRadius);
            canvas.Save();
            canvas.ClipPath(clipPath, SKClipOperation.Intersect, true);

            DrawSourceIcon(canvas, targetPath, contentRect);
            canvas.Restore();

            ApplyShellTemplate(canvas, contentRect, cornerRadius);

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.OpenWrite(cachePath);
            data.SaveTo(stream);
        }, cancellationToken).ConfigureAwait(false);

        return new IconPipelineResult { CacheFilePath = cachePath, Width = CanvasSize, Height = CanvasSize };
    }

    private static bool IsMacOsIconsAsset(string targetPath)
    {
        var normalized = targetPath.Replace('\\', '/');
        return normalized.Contains("/assets/icons/system/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/icons/system/", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ResolveDefaultShellPath()
    {
        var catalog = new Assets.AssetCatalogService();
        var path = catalog.ResolvePath("app-icon-shell");
        return File.Exists(path) ? path : null;
    }

    private static void DrawSourceIcon(SKCanvas canvas, string targetPath, SKRect rect)
    {
        if (File.Exists(targetPath) && targetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        {
            using var bitmap = SKBitmap.Decode(targetPath);
            if (bitmap is not null)
            {
                canvas.DrawBitmap(bitmap, rect);
                return;
            }
        }

        using var paint = new SKPaint
        {
            Color = new SKColor(0x2D, 0x9C, 0xDB),
            IsAntialias = true
        };
        canvas.DrawRoundRect(rect, rect.Width * CornerRadiusRatio, rect.Height * CornerRadiusRatio, paint);

        using var textPaint = new SKPaint
        {
            Color = SKColors.White,
            TextSize = rect.Width * 0.35f,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center
        };
        var letter = Path.GetFileNameWithoutExtension(targetPath)?.Length > 0
            ? Path.GetFileNameWithoutExtension(targetPath)![0].ToString().ToUpperInvariant()
            : "?";
        canvas.DrawText(letter, rect.MidX, rect.MidY + textPaint.TextSize * 0.35f, textPaint);
    }

    private void ApplyShellTemplate(SKCanvas canvas, SKRect rect, float cornerRadius)
    {
        if (!string.IsNullOrWhiteSpace(_appIconShellPath) && File.Exists(_appIconShellPath))
        {
            using var shell = SKBitmap.Decode(_appIconShellPath);
            if (shell is not null)
            {
                canvas.DrawBitmap(shell, new SKRect(0, 0, CanvasSize, CanvasSize));
                return;
            }
        }

        using var gloss = new SKPaint
        {
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(rect.Left, rect.Top),
                new SKPoint(rect.Left, rect.Bottom),
                [SKColors.White.WithAlpha(60), SKColors.Transparent],
                SKShaderTileMode.Clamp),
            IsAntialias = true
        };
        canvas.DrawRoundRect(rect, cornerRadius, cornerRadius, gloss);

        using var shadow = new SKPaint
        {
            Color = SKColors.Black.WithAlpha(40),
            IsAntialias = true,
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 12)
        };
        var shadowRect = rect;
        shadowRect.Offset(0, 6);
        canvas.DrawRoundRect(shadowRect, cornerRadius, cornerRadius, shadow);
    }
}
