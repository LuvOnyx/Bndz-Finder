using BndzFinder.Core.Models;
using BndzFinder.Core.Services;
using BndzFinder.Theming.Customization;
using BndzFinder.Theming.Glass;

namespace BndzFinder.Dock.Services;

public interface IDockAppearanceService
{
    DockAppearanceProfile GetCurrentProfile();
    GlassConfiguration GetGlassConfiguration();
    void ApplyGlassEffect(GlassEffect effect);
    void ApplyIconEffect(IconHoverEffect effect);
    void SetMagnification(bool enabled);
    void SetImmersion(bool enabled);
    void SetReflection(bool enabled, double opacity, double blur);
}

public sealed class DockAppearanceService : IDockAppearanceService
{
    private readonly ISettingsService _settings;
    private readonly IThemeResolver _themeResolver;

    public DockAppearanceService(ISettingsService settings, IThemeResolver? themeResolver = null)
    {
        _settings = settings;
        _themeResolver = themeResolver ?? new ThemeResolver();
    }

    public DockAppearanceProfile GetCurrentProfile() => BuildProfile();

    public GlassConfiguration GetGlassConfiguration() => BuildProfile().Glass;

    public void ApplyGlassEffect(GlassEffect effect)
    {
        _settings.Current.DockGlassEffect = effect;
        _ = _settings.SaveAsync();
    }

    public void ApplyIconEffect(IconHoverEffect effect)
    {
        _settings.Current.IconEffect = effect;
        _ = _settings.SaveAsync();
    }

    public void SetMagnification(bool enabled)
    {
        _settings.Current.IconMaxSize = enabled
            ? Math.Max(_settings.Current.IconSize + 16, 72)
            : _settings.Current.IconSize;
        _ = _settings.SaveAsync();
    }

    public void SetImmersion(bool enabled)
    {
        _settings.Current.IconImmersion = enabled;
        _ = _settings.SaveAsync();
    }

    public void SetReflection(bool enabled, double opacity, double blur)
    {
        _settings.Current.IconReflectionEnabled = enabled;
        _settings.Current.IconReflectionOpacity = opacity;
        _settings.Current.IconReflectionBlur = blur;
        _ = _settings.SaveAsync();
    }

    private DockAppearanceProfile BuildProfile()
    {
        var s = _settings.Current;
        return _themeResolver.BuildDockAppearance(new DockAppearanceInput
        {
            ThemeMode = s.ThemeMode switch
            {
                ThemeMode.Dark => ThemeModeKind.Dark,
                ThemeMode.Light => ThemeModeKind.Light,
                _ => ThemeModeKind.Auto
            },
            AccentColor = s.AccentColor,
            DpiScale = s.DpiScale,
            GlobalBlur = s.GlobalBlurValue,
            GlassEffect = s.DockGlassEffect switch
            {
                GlassEffect.Translucent => GlassEffectKind.Translucent,
                GlassEffect.Mica => GlassEffectKind.Mica,
                GlassEffect.LiquidGlass => GlassEffectKind.LiquidGlass,
                _ => GlassEffectKind.Acrylic
            },
            IconReflectionEnabled = s.IconReflectionEnabled,
            IconReflectionOpacity = s.IconReflectionOpacity,
            IconReflectionBlur = s.IconReflectionBlur,
            IconEffect = s.IconEffect switch
            {
                IconHoverEffect.None => IconEffectKind.None,
                IconHoverEffect.Select => IconEffectKind.Select,
                IconHoverEffect.Light => IconEffectKind.Light,
                IconHoverEffect.ScaleLight => IconEffectKind.ScaleLight,
                _ => IconEffectKind.Scale
            },
            IconImmersion = s.IconImmersion,
            BaseIconSize = s.IconSize,
            MaxIconSize = s.IconMaxSize
        });
    }
}

public interface IDockCompositor
{
    void ApplyGlass(GlassConfiguration config);
    void RenderIcons(IReadOnlyList<ViewModels.DockIconViewModel> icons);
}

public sealed class DockCompositor : IDockCompositor
{
    private GlassConfiguration? _currentGlass;

    public void ApplyGlass(GlassConfiguration config) => _currentGlass = config;

    public GlassConfiguration? CurrentGlass => _currentGlass;

    public void RenderIcons(IReadOnlyList<ViewModels.DockIconViewModel> icons) => _ = icons;
}

