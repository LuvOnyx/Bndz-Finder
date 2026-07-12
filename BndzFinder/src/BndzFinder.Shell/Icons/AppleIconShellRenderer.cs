using SkiaSharp;

namespace BndzFinder.Shell.Icons;

/// <summary>
/// Renders the macOS app-icon shell (squircle mask, gloss, shadow) per Apple App Icon Template proportions.
/// Optional PNG overlay from assets/templates/apple-design-resources/app-icon-shell.png takes precedence.
/// </summary>
public static class AppleIconShellRenderer
{
    public static void ApplyShell(SKCanvas canvas, SKRect contentRect, float cornerRadius, string? shellOverlayPath)
    {
        if (!string.IsNullOrWhiteSpace(shellOverlayPath) && File.Exists(shellOverlayPath))
        {
            using var shell = SKBitmap.Decode(shellOverlayPath);
            if (shell is not null)
            {
                canvas.DrawBitmap(shell, new SKRect(0, 0, shell.Width, shell.Height));
                return;
            }
        }

        DrawProgrammaticShell(canvas, contentRect, cornerRadius);
    }

    public static SKPath CreateSquirclePath(SKRect rect, float cornerRadius)
    {
        var path = new SKPath();
        path.AddRoundRect(rect, cornerRadius, cornerRadius);
        return path;
    }

    private static void DrawProgrammaticShell(SKCanvas canvas, SKRect rect, float cornerRadius)
    {
        var shadowRect = rect;
        shadowRect.Offset(0, 8);
        using var shadowPaint = new SKPaint
        {
            Color = SKColors.Black.WithAlpha(55),
            IsAntialias = true,
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 16)
        };
        canvas.DrawRoundRect(shadowRect, cornerRadius, cornerRadius, shadowPaint);

        using var rimPaint = new SKPaint
        {
            Color = SKColors.White.WithAlpha(28),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2f
        };
        canvas.DrawRoundRect(rect, cornerRadius, cornerRadius, rimPaint);

        using var glossPaint = new SKPaint
        {
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(rect.Left, rect.Top),
                new SKPoint(rect.Left, rect.Top + rect.Height * 0.55f),
                [SKColors.White.WithAlpha(72), SKColors.White.WithAlpha(8), SKColors.Transparent],
                [0f, 0.35f, 1f],
                SKShaderTileMode.Clamp),
            IsAntialias = true
        };
        canvas.DrawRoundRect(rect, cornerRadius, cornerRadius, glossPaint);
    }
}
