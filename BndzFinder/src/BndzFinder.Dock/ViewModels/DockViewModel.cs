using BndzFinder.Core.Models;
using BndzFinder.Core.Services;
using BndzFinder.Core.Settings;
using BndzFinder.Shell.Assets;
using BndzFinder.Shell.Badges;
using BndzFinder.Shell.Dock;
using BndzFinder.Shell.Icons;
using BndzFinder.Shell.Services;
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
    private readonly IDockBehaviorService _behavior;
    private readonly BadgePollingService _badges;
    private readonly ProgressBarMirrorService _progress;
    private readonly WindowPreviewCoordinator _preview;
    private readonly IShellOverlayController? _overlays;
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
    [ObservableProperty] private bool _pointerNearEdge;
    [ObservableProperty] private IReadOnlyDictionary<string, int?> _badgeCounts = new Dictionary<string, int?>();

    public bool PreviewEnabled => _settings.Current.PreviewOn;

    public DockViewModel(
        ISettingsService settings,
        IPremiumDockLayoutEngine? layoutEngine = null,
        IIconPipeline? iconPipeline = null,
        IThemeResolver? themeResolver = null,
        IDockBehaviorService? behavior = null,
        BadgePollingService? badges = null,
        ProgressBarMirrorService? progress = null,
        IShellOverlayController? overlays = null)
    {
        _settings = settings;
        _layoutEngine = layoutEngine ?? new PremiumDockLayoutEngine();
        _iconPipeline = iconPipeline ?? new MacStyleIconPipeline();
        _themeResolver = themeResolver ?? new ThemeResolver();
        _behavior = behavior ?? new DockBehaviorService();
        _badges = badges ?? new BadgePollingService();
        _progress = progress ?? new ProgressBarMirrorService();
        _preview = new WindowPreviewCoordinator(settings.Current.PreviewDelayMs, settings.Current.PreviewSize);
        _overlays = overlays;
        _settings.SettingsChanged += (_, _) => RefreshAll();
        _badges.CountsUpdated += (_, e) => BadgeCounts = e.Counts;
        _badges.Start(TimeSpan.FromSeconds(5));
        SeedDefaultItems();
        RefreshAll();
    }

    partial void OnCursorPositionChanged(double value) => RefreshLayout();
    partial void OnHoveredIndexChanged(int? value) => RefreshLayout();
    partial void OnMagnificationEnabledChanged(bool value) => RefreshLayout();
    partial void OnPointerNearEdgeChanged(bool value) => UpdateVisibility();

    [RelayCommand]
    public void OnPointerMoved(double position) => CursorPosition = position;

    [RelayCommand]
    public void OnPointerExited()
    {
        HoveredIndex = null;
        CursorPosition = -9999;
    }

    [RelayCommand]
    public void OnIconPointerEntered(int index)
    {
        HoveredIndex = index;
        if (PreviewEnabled)
            _ = _preview.OnIconHoveredAsync(index, CancellationToken.None);
    }

    [RelayCommand]
    public async Task HandleItemClickAsync(DockItem item)
    {
        switch (item.Kind)
        {
            case DockItemKind.SystemFinder:
            case DockItemKind.SystemLaunchpad:
            case DockItemKind.SystemCalendar:
            case DockItemKind.SystemTrash:
            case DockItemKind.SystemWeather:
            case DockItemKind.SystemPreferences:
                _overlays?.HandleHotkey(item.Kind switch
                {
                    DockItemKind.SystemLaunchpad => "hotkeypad",
                    DockItemKind.SystemPreferences => "hotkeyfinder",
                    _ => "hotkeyDock"
                });
                break;
            case DockItemKind.Folder:
                _ = item;
                break;
            default:
                await LaunchItemAsync(item);
                break;
        }
    }

    [RelayCommand]
    public async Task PinItemAsync(string path)
    {
        if (_settings.Current.LockIcons) return;
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
        if (item.Kind == DockItemKind.Application || item.Kind == DockItemKind.File)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(item.TargetPath) { UseShellExecute = true });
            _runningApps.Add(item.Id);
            RefreshLayout();
        }
        await Task.CompletedTask;
    }

    public void RefreshAll()
    {
        RefreshAppearance();
        RefreshLayout();
        UpdateVisibility();
    }

    private void SeedDefaultItems()
    {
        if (_settings.Current.DockItems.Count == 0)
            _settings.Current.DockItems = _behavior.EnsureDefaultItems(_settings.Current).ToList();
    }

    private void UpdateVisibility()
    {
        IsVisible = _behavior.ShouldShowDock(_settings.Current, PointerNearEdge, _runningApps.Count > 0);
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
            DockOpacity = s.DockOpacity,
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
            IconCachePath = ResolveIconPath(item.Item),
            DisplayName = item.Item.DisplayName ?? Path.GetFileNameWithoutExtension(item.Item.TargetPath),
            LabelOpacity = item.IsHovered ? 1.0 : 0.0,
            BadgeCount = ResolveBadge(item.Item),
            Progress = _progress.GetProgress(item.Item.TargetPath)
        }).ToList();
    }

    private string ResolveIconPath(DockItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.CustomIconPath) && File.Exists(item.CustomIconPath))
            return item.CustomIconPath;
        var catalog = new AssetCatalogService();
        var assetId = item.Kind switch
        {
            DockItemKind.SystemFinder => "icon-finder",
            DockItemKind.SystemLaunchpad => "icon-launchpad",
            DockItemKind.SystemCalendar => "icon-calendar",
            DockItemKind.SystemTrash => "icon-trash",
            DockItemKind.SystemWeather => "icon-weather",
            DockItemKind.SystemPreferences => "icon-preferences",
            _ => null
        };
        if (assetId is not null)
        {
            var path = catalog.ResolvePath(assetId);
            if (File.Exists(path)) return path;
        }
        return _iconPipeline.GetCachePath(item.TargetPath);
    }

    private int? ResolveBadge(DockItem item)
    {
        var name = item.DisplayName ?? item.TargetPath;
        foreach (var pair in BadgeCounts)
        {
            if (name.Contains(pair.Key, StringComparison.OrdinalIgnoreCase))
                return pair.Value;
        }
        return null;
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
    public int? BadgeCount { get; init; }
    public double Progress { get; init; }
    public double RenderSize => Layout.Size;
    public double RenderX => Layout.X;
    public double RenderY => Layout.Y;
    public double LightOverlayOpacity => Layout.LightIntensity;
    public double SelectRingOpacity => Layout.SelectGlow;
    public double ReflectionOpacity => Layout.ReflectionOpacity;
    public double ShadowOpacity => Layout.ShadowOpacity;
    public bool ShowRunningDot => Layout.ShowRunningIndicator && Layout.Item.IsPinned;
}
