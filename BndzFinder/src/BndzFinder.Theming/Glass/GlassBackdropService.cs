using BndzFinder.Core.Design;

namespace BndzFinder.Theming.Glass;

public sealed record GlassConfiguration
{
    public required GlassEffectKind Effect { get; init; }
    public double BlurAmount { get; init; } = 0.6;
    public double Opacity { get; init; } = 0.82;
    public double Saturation { get; init; } = 1.15;
    public double Luminosity { get; init; } = 0.95;
    public double TintOpacity { get; init; } = 0.04;
    public string TintColor { get; init; } = "#FFFFFF";
    public double CornerRadius { get; init; } = 24;
    public double BorderOpacity { get; init; } = 0.12;
    public double ShadowOpacity { get; init; } = 0.18;
    public double ShadowBlur { get; init; } = 24;
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
    public double Distortion { get; init; } = 0.35;
    public double Refraction { get; init; } = 0.22;
    public double EdgeHighlight { get; init; } = 0.45;
    public double NoiseScale { get; init; } = 1.8;
}

public interface IGlassBackdropService
{
    GlassConfiguration Resolve(GlassEffectKind effect, GlassCustomizationInput input);
    GlassConfiguration ResolveLiquidGlass(LiquidGlassParameters parameters, GlassCustomizationInput input);
}

public sealed class GlassCustomizationInput
{
    public double GlobalBlur { get; init; } = 0.6;
    public double Opacity { get; init; } = 0.82;
    public ThemeModeKind ThemeMode { get; init; } = ThemeModeKind.Auto;
    public string AccentColor { get; init; } = "#0078D4";
    public bool IsDark { get; init; }
}

public enum ThemeModeKind
{
    Light = 0,
    Dark = 1,
    Auto = 2
}

public sealed class GlassBackdropService : IGlassBackdropService
{
    public GlassConfiguration Resolve(GlassEffectKind effect, GlassCustomizationInput input)
    {
        var isDark = input.IsDark || (input.ThemeMode == ThemeModeKind.Dark);
        var tint = isDark ? "#1A1A1A" : "#FFFFFF";
        var borderOpacity = isDark ? 0.18 : 0.12;

        return effect switch
        {
            GlassEffectKind.Translucent => new GlassConfiguration
            {
                Effect = GlassEffectKind.Translucent,
                BlurAmount = 0,
                Opacity = 0.72,
                Saturation = 1.0,
                TintColor = tint,
                TintOpacity = isDark ? 0.55 : 0.35,
                CornerRadius = AppleDesignMetrics.DockPillCornerRadius,
                BorderOpacity = borderOpacity,
                ShadowOpacity = 0.12
            },
            GlassEffectKind.Acrylic => new GlassConfiguration
            {
                Effect = GlassEffectKind.Acrylic,
                BlurAmount = input.GlobalBlur,
                Opacity = input.Opacity,
                Saturation = 1.12,
                Luminosity = isDark ? 0.88 : 0.98,
                TintColor = tint,
                TintOpacity = isDark ? 0.08 : 0.04,
                CornerRadius = AppleDesignMetrics.DockPillCornerRadius,
                BorderOpacity = borderOpacity,
                ShadowOpacity = 0.16,
                ShadowBlur = 20
            },
            GlassEffectKind.Mica => new GlassConfiguration
            {
                Effect = GlassEffectKind.Mica,
                BlurAmount = Math.Clamp(input.GlobalBlur * 0.85, 0.3, 1.0),
                Opacity = Math.Clamp(input.Opacity * 0.95, 0.7, 0.95),
                Saturation = 1.05,
                Luminosity = isDark ? 0.92 : 1.0,
                TintColor = input.AccentColor,
                TintOpacity = 0.03,
                CornerRadius = AppleDesignMetrics.DockPillCornerRadius,
                BorderOpacity = borderOpacity * 0.8,
                ShadowOpacity = 0.14
            },
            GlassEffectKind.LiquidGlass => ResolveLiquidGlass(new LiquidGlassParameters(), input),
            _ => Resolve(GlassEffectKind.Acrylic, input)
        };
    }

    public GlassConfiguration ResolveLiquidGlass(LiquidGlassParameters parameters, GlassCustomizationInput input)
    {
        var acrylic = Resolve(GlassEffectKind.Acrylic, input);
        return acrylic with
        {
            Effect = GlassEffectKind.LiquidGlass,
            BlurAmount = Math.Clamp(input.GlobalBlur * 1.15, 0.4, 1.0),
            Saturation = 1.25,
            LiquidGlass = parameters
        };
    }
}
