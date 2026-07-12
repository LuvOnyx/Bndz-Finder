using BndzFinder.Core.Models;
using BndzFinder.Theming.Glass;

namespace BndzFinder.Theming.Customization;

public interface IAccentColorService
{
    RgbaColor ParseAccent(string hex);
    RgbaColor ResolveForeground(RgbaColor background, bool isDark);
    string ToWinUiResourceKey(RgbaColor accent);
}

public sealed class RgbaColor
{
    public byte R { get; init; }
    public byte G { get; init; }
    public byte B { get; init; }
    public byte A { get; init; } = 255;

    public double Luminance => (0.299 * R + 0.587 * G + 0.114 * B) / 255.0;
}

public sealed class AccentColorService : IAccentColorService
{
    public RgbaColor ParseAccent(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return new RgbaColor { R = 0, G = 120, B = 212 };

        hex = hex.TrimStart('#');
        if (hex.Length == 6)
        {
            return new RgbaColor
            {
                R = Convert.ToByte(hex[..2], 16),
                G = Convert.ToByte(hex[2..4], 16),
                B = Convert.ToByte(hex[4..6], 16)
            };
        }

        if (hex.Length == 8)
        {
            return new RgbaColor
            {
                R = Convert.ToByte(hex[..2], 16),
                G = Convert.ToByte(hex[2..4], 16),
                B = Convert.ToByte(hex[4..6], 16),
                A = Convert.ToByte(hex[6..8], 16)
            };
        }

        return new RgbaColor { R = 0, G = 120, B = 212 };
    }

    public RgbaColor ResolveForeground(RgbaColor background, bool isDark) =>
        isDark || background.Luminance < 0.5
            ? new RgbaColor { R = 255, G = 255, B = 255, A = 230 }
            : new RgbaColor { R = 20, G = 20, B = 20, A = 230 };

    public string ToWinUiResourceKey(RgbaColor accent) =>
        $"Accent_{accent.R:X2}{accent.G:X2}{accent.B:X2}";
}

public interface IThemeResolver
{
    bool ResolveIsDark(ThemeModeKind mode, bool systemIsDark);
    DockAppearanceProfile BuildDockAppearance(DockAppearanceInput input);
}

public sealed class DockAppearanceInput
{
    public ThemeModeKind ThemeMode { get; init; }
    public bool SystemIsDark { get; init; }
    public string AccentColor { get; init; } = "#0078D4";
    public double DpiScale { get; init; } = 1.0;
    public double GlobalBlur { get; init; } = 0.6;
    public double DockOpacity { get; init; } = 0.82;
    public GlassEffectKind GlassEffect { get; init; } = GlassEffectKind.Acrylic;
    public bool IconReflectionEnabled { get; init; } = true;
    public double IconReflectionOpacity { get; init; } = 0.35;
    public double IconReflectionBlur { get; init; } = 0.5;
    public IconEffectKind IconEffect { get; init; } = IconEffectKind.Scale;
    public bool IconImmersion { get; init; }
    public int BaseIconSize { get; init; } = 48;
    public int MaxIconSize { get; init; } = 72;
}

public sealed class DockAppearanceProfile
{
    public required Glass.GlassConfiguration Glass { get; init; }
    public required RgbaColor Accent { get; init; }
    public required RgbaColor Foreground { get; init; }
    public bool IsDark { get; init; }
    public double DpiScale { get; init; }
    public IconEffectKind IconEffect { get; init; }
    public bool IconImmersion { get; init; }
    public bool IconReflectionEnabled { get; init; }
    public double IconReflectionOpacity { get; init; }
    public double IconReflectionBlur { get; init; }
    public int BaseIconSize { get; init; }
    public int MaxIconSize { get; init; }
    public double ScaledCornerRadius => Glass.CornerRadius * DpiScale;
    public double ScaledBaseIconSize => BaseIconSize * DpiScale;
    public double ScaledMaxIconSize => MaxIconSize * DpiScale;
}

public sealed class ThemeResolver : IThemeResolver
{
    private readonly IAccentColorService _accent;
    private readonly Glass.IGlassBackdropService _glass;

    public ThemeResolver(IAccentColorService? accent = null, Glass.IGlassBackdropService? glass = null)
    {
        _accent = accent ?? new AccentColorService();
        _glass = glass ?? new Glass.GlassBackdropService();
    }

    public bool ResolveIsDark(ThemeModeKind mode, bool systemIsDark) =>
        mode switch
        {
            ThemeModeKind.Dark => true,
            ThemeModeKind.Light => false,
            _ => systemIsDark
        };

    public DockAppearanceProfile BuildDockAppearance(DockAppearanceInput input)
    {
        var isDark = ResolveIsDark(input.ThemeMode, input.SystemIsDark);
        var accent = _accent.ParseAccent(input.AccentColor);
        var glass = _glass.Resolve(input.GlassEffect, new Glass.GlassCustomizationInput
        {
            GlobalBlur = input.GlobalBlur,
            Opacity = input.DockOpacity,
            ThemeMode = input.ThemeMode,
            AccentColor = input.AccentColor,
            IsDark = isDark
        });

        return new DockAppearanceProfile
        {
            Glass = glass,
            Accent = accent,
            Foreground = _accent.ResolveForeground(accent, isDark),
            IsDark = isDark,
            DpiScale = input.DpiScale,
            IconEffect = input.IconEffect,
            IconImmersion = input.IconImmersion,
            IconReflectionEnabled = input.IconReflectionEnabled,
            IconReflectionOpacity = input.IconReflectionOpacity,
            IconReflectionBlur = input.IconReflectionBlur,
            BaseIconSize = input.BaseIconSize,
            MaxIconSize = input.MaxIconSize
        };
    }
}
