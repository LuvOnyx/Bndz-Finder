using BndzFinder.Core.Models;
using BndzFinder.Core.Services;
using BndzFinder.Core.Settings;
using BndzFinder.Shell.Dock;
using BndzFinder.Shell.Icons;
using BndzFinder.Theming.Customization;
using BndzFinder.Theming.Glass;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BndzFinder.Dock.ViewModels;

public partial class DockViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IPremiumDockLayoutEngine _layoutEngine;
    private readonly IIconPipeline _iconPipeline;
    private readonly IThemeResolver _themeResolver;
    private readonly HashSet<string> _runningApps = new(StringComparer.OrdinalIgnoreCase);

    [ObservableProperty] private IReadOnlyList<PremiumDockLayoutItem> _layoutItems = [];
    [ObservableProperty] private IReadOnlyList<DockIconViewModel> _iconViewModels = [];
    [ObservableProperty] private double _cursorPosition = -9999;
    [ObservableProperty] private int? _hoveredIndex;
    [ObservableProperty] private bool _isVisible = true;
    [ObservableProperty] private bool _magnificationEnabled = true;
    [ObservableProperty] private DockAppearanceProfile? _appearance;
    [ObservableProperty] private double _dockBarHeight = 64;
    [ObservableProperty] private double _dockBarWidth = 600;

    public DockViewModel(
        ISettingsService settings,
        IPremiumDockLayoutEngine? layoutEngine = null,
        IIconPipeline? iconPipeline = null,
        IThemeResolver? themeResolver = null)
    {
        _settings = settings;
        _layoutEngine = layoutEngine ?? new PremiumDockLayoutEngine();
        _iconPipeline = iconPipeline ?? new MacStyleIconPipeline();
        _themeResolver = themeResolver ?? new ThemeResolver();
        _settings.SettingsChanged += (_, _) => RefreshAll();
        RefreshAll();
    }

    partial void OnCursorPositionChanged(double value) => RefreshLayout();
    partial void OnHoveredIndexChanged(int? value) => RefreshLayout();
    partial void OnMagnificationEnabledChanged(bool value) => RefreshLayout();

    [RelayCommand]
    public void OnPointerMoved(double position)
    {
        CursorPosition = position;
    }

    [RelayCommand]
    public void OnPointerExited()
    {
        HoveredIndex = null;
        CursorPosition = -9999;
    }

    [RelayCommand]
    public void OnIconPointerEntered(int index) => HoveredIndex = index;

    [RelayCommand]
    public async Task PinItemAsync(string path)
    {
        var settings = _settings.Current;
        settings.DockItems.Add(new DockItem
        {
            Id = Guid.NewGuid().ToString("N"),
            Kind = DockItemKind.Application,
            TargetPath = path,
            DisplayName = Path.GetFileNameWithoutExtension(path),
            IsPinned = true,
            SortOrder = settings.DockItems.Count
        });
        await _iconPipeline.ProcessAsync(path).ConfigureAwait(false);
        await _settings.SaveAsync().ConfigureAwait(false);
        RefreshAll();
    }

    [RelayCommand]
    public async Task LaunchItemAsync(DockItem item)
    {
        if (!OperatingSystem.IsWindows()) return;
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(item.TargetPath) { UseShellExecute = true });
        _runningApps.Add(item.Id);
        RefreshLayout();
        await Task.CompletedTask;
    }

    [RelayCommand]
    public void SetGlassEffect(GlassEffect effect)
    {
        _settings.Current.DockGlassEffect = effect;
        RefreshAppearance();
    }

    [RelayCommand]
    public void SetIconEffect(IconHoverEffect effect)
    {
        _settings.Current.IconEffect = effect;
        RefreshLayout();
    }

    public void RefreshAll()
    {
        RefreshAppearance();
        RefreshLayout();
    }

    private void RefreshAppearance()
    {
        var s = _settings.Current;
        Appearance = _themeResolver.BuildDockAppearance(new DockAppearanceInput
        {
            ThemeMode = MapThemeMode(s.ThemeMode),
            SystemIsDark = false,
            AccentColor = s.AccentColor,
            DpiScale = s.DpiScale,
            GlobalBlur = s.GlobalBlurValue,
            DockOpacity = 0.82,
            GlassEffect = MapGlassEffect(s.DockGlassEffect),
            IconReflectionEnabled = s.IconReflectionEnabled,
            IconReflectionOpacity = s.IconReflectionOpacity,
            IconReflectionBlur = s.IconReflectionBlur,
            IconEffect = MapIconEffect(s.IconEffect),
            IconImmersion = s.IconImmersion,
            BaseIconSize = s.IconSize,
            MaxIconSize = s.IconMaxSize
        });

        if (Appearance is not null)
        {
            DockBarHeight = Appearance.ScaledBaseIconSize + 24 * Appearance.DpiScale;
            DockBarWidth = Math.Max(400, (s.DockItems.Count * (s.IconSize + s.IconSpace) + 48) * Appearance.DpiScale);
        }
    }

    private void RefreshLayout()
    {
        var s = _settings.Current;
        LayoutItems = _layoutEngine.ComputeLayout(new PremiumDockLayoutRequest
        {
            Items = s.DockItems,
            CursorPosition = CursorPosition,
            BaseIconSize = s.IconSize,
            MaxIconSize = s.IconMaxSize,
            IconSpacing = s.IconSpace,
            IconEffect = MapIconEffect(s.IconEffect),
            IconImmersion = s.IconImmersion,
            MagnificationEnabled = MagnificationEnabled,
            ReflectionEnabled = s.IconReflectionEnabled,
            ReflectionOpacity = s.IconReflectionOpacity,
            ReflectionBlur = s.IconReflectionBlur,
            RunningAppIds = _runningApps,
            HoveredIndex = HoveredIndex
        });

        IconViewModels = LayoutItems.Select(item => new DockIconViewModel
        {
            Layout = item,
            IconCachePath = _iconPipeline.GetCachePath(item.Item.TargetPath),
            DisplayName = item.Item.DisplayName ?? Path.GetFileNameWithoutExtension(item.Item.TargetPath),
            LabelOpacity = item.IsHovered ? 1.0 : 0.0
        }).ToList();
    }

    private static ThemeModeKind MapThemeMode(ThemeMode mode) => mode switch
    {
        ThemeMode.Dark => ThemeModeKind.Dark,
        ThemeMode.Light => ThemeModeKind.Light,
        _ => ThemeModeKind.Auto
    };

    private static GlassEffectKind MapGlassEffect(GlassEffect effect) => effect switch
    {
        GlassEffect.Translucent => GlassEffectKind.Translucent,
        GlassEffect.Mica => GlassEffectKind.Mica,
        GlassEffect.LiquidGlass => GlassEffectKind.LiquidGlass,
        _ => GlassEffectKind.Acrylic
    };

    private static IconEffectKind MapIconEffect(IconHoverEffect effect) => effect switch
    {
        IconHoverEffect.None => IconEffectKind.None,
        IconHoverEffect.Select => IconEffectKind.Select,
        IconHoverEffect.Light => IconEffectKind.Light,
        IconHoverEffect.ScaleLight => IconEffectKind.ScaleLight,
        _ => IconEffectKind.Scale
    };
}

public sealed class DockIconViewModel
{
    public required PremiumDockLayoutItem Layout { get; init; }
    public required string IconCachePath { get; init; }
    public required string DisplayName { get; init; }
    public double LabelOpacity { get; init; }
    public double RenderSize => Layout.Size;
    public double RenderX => Layout.X;
    public double RenderY => Layout.Y;
    public double LightOverlayOpacity => Layout.LightIntensity;
    public double SelectRingOpacity => Layout.SelectGlow;
    public double ReflectionOpacity => Layout.ReflectionOpacity;
    public double ShadowOpacity => Layout.ShadowOpacity;
    public bool ShowRunningDot => Layout.ShowRunningIndicator && Layout.Item.IsPinned;
}
