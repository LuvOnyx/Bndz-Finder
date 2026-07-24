using BndzFinder.Core.Design;

namespace BndzFinder.Theming.Glass;

public sealed record GlassConfiguration
{
    public required GlassEffectKind Effect { get; init; }
    public double BlurAmount { get; init; } = 0.75;
    public double Opacity { get; init; } = 0.78;
    public double Saturation { get; init; } = 1.2;
    public double Luminosity { get; init; } = 0.92;
    public double TintOpacity { get; init; } = 0.28;
    public string TintColor { get; init; } = AppleDesignMetrics.DockGlassTintDark;
    public double CornerRadius { get; init; } = AppleDesignMetrics.DockPillCornerRadius;
    public double BorderOpacity { get; init; } = 0.22;
    public double ShadowOpacity { get; init; } = 0.35;
    public double ShadowBlur { get; init; } = 28;
    public double HighlightOpacity { get; init; } = 0.28;
    public LiquidGlassParameters? LiquidGlass { get; init; }
}

public enum GlassEffectKind
{
    Translucent = 0,
    Acrylic = 1,
    Mica = 2,
    LiquidGlass = 3
}

public sealed class LiquidGlassParameters
{
    public double Distortion { get; init; } = 0.28;
    public double Refraction { get; init; } = 0.18;
    public double EdgeHighlight { get; init; } = 0.55;
    public double NoiseScale { get; init; } = 1.6;
}

public interface IGlassBackdropService
{
    GlassConfiguration Resolve(GlassEffectKind effect, GlassCustomizationInput input);
    GlassConfiguration ResolveLiquidGlass(LiquidGlassParameters parameters, GlassCustomizationInput input);
}

public sealed class GlassCustomizationInput
{
    public double GlobalBlur { get; init; } = 0.75;
    public double Opacity { get; init; } = 0.78;
    public double CornerRadius { get; init; } = AppleDesignMetrics.DockPillCornerRadius;
    public ThemeModeKind ThemeMode { get; init; } = ThemeModeKind.Auto;
    public string AccentColor { get; init; } = "#0A84FF";
    public bool IsDark { get; init; }
}

public enum ThemeModeKind
{
    Light = 0,
    Dark = 1,
    Auto = 2
}

/// <summary>
/// Resolves macOS Tahoe / Sequoia frosted-glass dock parameters.
/// Key: low tint opacity so acrylic blur reads as glass, not a solid slab.
/// </summary>
public sealed class GlassBackdropService : IGlassBackdropService
{
    public GlassConfiguration Resolve(GlassEffectKind effect, GlassCustomizationInput input)
    {
        var isDark = input.IsDark || input.ThemeMode == ThemeModeKind.Dark;
        var tint = isDark ? AppleDesignMetrics.DockGlassTintDark : AppleDesignMetrics.DockGlassTintLight;
        var corner = input.CornerRadius > 0 ? input.CornerRadius : AppleDesignMetrics.DockPillCornerRadius;

        return effect switch
        {
            GlassEffectKind.Translucent => new GlassConfiguration
            {
                Effect = GlassEffectKind.Translucent,
                BlurAmount = 0,
                Opacity = isDark ? 0.62 : 0.72,
                Saturation = 1.0,
                TintColor = tint,
                TintOpacity = isDark ? 0.42 : 0.55,
                CornerRadius = corner,
                BorderOpacity = isDark ? 0.2 : 0.28,
                ShadowOpacity = 0.28,
                HighlightOpacity = 0.18
            },
            GlassEffectKind.Acrylic => new GlassConfiguration
            {
                Effect = GlassEffectKind.Acrylic,
                BlurAmount = Math.Clamp(input.GlobalBlur, 0.55, 1.0),
                Opacity = Math.Clamp(input.Opacity, 0.65, 0.88),
                Saturation = 1.18,
                Luminosity = isDark ? 0.9 : 0.98,
                TintColor = tint,
                // Critical: keep tint light enough that wallpaper blur shows through.
                TintOpacity = isDark ? 0.32 : 0.22,
                CornerRadius = corner,
                BorderOpacity = isDark ? 0.24 : 0.32,
                ShadowOpacity = 0.38,
                ShadowBlur = 32,
                HighlightOpacity = isDark ? 0.28 : 0.42
            },
            GlassEffectKind.Mica => new GlassConfiguration
            {
                Effect = GlassEffectKind.Mica,
                BlurAmount = Math.Clamp(input.GlobalBlur * 0.9, 0.45, 1.0),
                Opacity = Math.Clamp(input.Opacity * 0.95, 0.7, 0.9),
                Saturation = 1.08,
                Luminosity = isDark ? 0.93 : 1.0,
                TintColor = tint,
                TintOpacity = isDark ? 0.26 : 0.18,
                CornerRadius = corner,
                BorderOpacity = isDark ? 0.18 : 0.26,
                ShadowOpacity = 0.3,
                HighlightOpacity = 0.22
            },
            GlassEffectKind.LiquidGlass => ResolveLiquidGlass(new LiquidGlassParameters(), input),
            _ => Resolve(GlassEffectKind.Acrylic, input)
        };
    }

    public GlassConfiguration ResolveLiquidGlass(LiquidGlassParameters parameters, GlassCustomizationInput input)
    {
        var acrylic = Resolve(GlassEffectKind.Acrylic, input);
        var isDark = input.IsDark || input.ThemeMode == ThemeModeKind.Dark;
        return acrylic with
        {
            Effect = GlassEffectKind.LiquidGlass,
            BlurAmount = Math.Clamp(input.GlobalBlur * 1.2, 0.6, 1.0),
            Saturation = 1.28,
            TintOpacity = isDark ? 0.26 : 0.18,
            HighlightOpacity = 0.35 + parameters.EdgeHighlight * 0.25,
            LiquidGlass = parameters
        };
    }
}
