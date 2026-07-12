using BndzFinder.Core.Models;
using BndzFinder.Core.Services;
using BndzFinder.Core.Settings;
using BndzFinder.Interop;
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
    private readonly IWindowCaptureService? _capture;
    private readonly IRunningAppSyncService _runningAppSync;
    private readonly HashSet<string> _runningApps = new(StringComparer.OrdinalIgnoreCase);
    private readonly System.Timers.Timer _runningAppTimer = new(2000) { AutoReset = true, Enabled = false };
    private IReadOnlyList<DockItem> _effectiveItems = [];
    private DateTimeOffset? _hideAfterUtc;

    /// <summary>Set by DockWindow to marshal layout refresh onto the UI thread.</summary>
    public Action? UiRefresh { get; set; }

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
    [ObservableProperty] private string? _activeFolderStackPath;

    public bool PreviewEnabled => _settings.Current.PreviewOn;

    public event Action<long>? PreviewShowRequested;
    public event Action? PreviewHideRequested;

    public IWindowCaptureService? CaptureService => _capture;

    /// <summary>When set, the dock window should hide after this UTC time (hide-delay modes).</summary>
    public DateTimeOffset? HideAfterUtc => _hideAfterUtc;

    public DockViewModel(
        ISettingsService settings,
        IPremiumDockLayoutEngine? layoutEngine = null,
        IIconPipeline? iconPipeline = null,
        IThemeResolver? themeResolver = null,
        IDockBehaviorService? behavior = null,
        BadgePollingService? badges = null,
        ProgressBarMirrorService? progress = null,
        IShellOverlayController? overlays = null,
        IWindowCaptureService? capture = null,
        IRunningAppSyncService? runningAppSync = null)
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
        _capture = capture;
        _runningAppSync = runningAppSync ?? new RunningAppSyncService();
        _preview.PreviewShowRequested += hwnd => PreviewShowRequested?.Invoke(hwnd);
        _preview.PreviewHideRequested += () => PreviewHideRequested?.Invoke();
        _settings.SettingsChanged += (_, _) => RefreshAll();
        _badges.CountsUpdated += (_, e) => BadgeCounts = e.Counts;
        _badges.Start(TimeSpan.FromSeconds(5));
        _runningAppTimer.Elapsed += (_, _) => RefreshRunningApps();
        SeedDefaultItems();
        RefreshAll();
        if (OperatingSystem.IsWindows())
            _runningAppTimer.Start();
    }

    partial void OnCursorPositionChanged(double value) => RefreshLayout();
    partial void OnHoveredIndexChanged(int? value) => RefreshLayout();
    partial void OnMagnificationEnabledChanged(bool value) => RefreshLayout();
    partial void OnPointerNearEdgeChanged(bool value) => UpdateVisibility();

    partial void OnIsVisibleChanged(bool value) => _overlays?.SetDockVisible(value);

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
        if (!PreviewEnabled || index < 0 || index >= LayoutItems.Count) return;
        var item = LayoutItems[index].Item;
        var hwnd = WindowEnumerationService.FindMainWindowForExecutable(item.TargetPath);
        if (hwnd != nint.Zero)
            _ = _preview.OnIconHoveredAsync(hwnd, CancellationToken.None);
    }

    public void OnIconPointerExited() => _preview.OnIconExited();

    [RelayCommand]
    public async Task HandleItemClickAsync(DockItem item)
    {
        switch (item.Kind)
        {
            case DockItemKind.SystemFinder:
                _overlays?.HandleHotkey("hotkeyfinder");
                break;
            case DockItemKind.SystemLaunchpad:
                _overlays?.HandleHotkey("hotkeypad");
                break;
            case DockItemKind.SystemPreferences:
                _overlays?.ShowPreferences();
                break;
            case DockItemKind.SystemCalendar:
                if (OperatingSystem.IsWindows())
                    _ = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("outlookcal:") { UseShellExecute = true });
                break;
            case DockItemKind.SystemTrash:
                if (OperatingSystem.IsWindows())
                    _ = System.Diagnostics.Process.Start("explorer", "shell:RecycleBinFolder");
                break;
            case DockItemKind.SystemWeather:
                _overlays?.HandleHotkey("hotkeyfinder");
                break;
            case DockItemKind.Folder:
                ActiveFolderStackPath = item.TargetPath;
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
        RefreshRunningApps();
        RefreshAppearance();
        RefreshLayout();
        UpdateVisibility();
        _ = EnsureIconsAsync();
    }

    private void RefreshRunningApps()
    {
        var running = _runningAppSync.GetRunningApps(_settings.Current.DockAppBlacklist);
        _effectiveItems = _runningAppSync.MergeDockItems(_settings.Current.DockItems, running, out var runningIds);
        _runningApps.Clear();
        foreach (var id in runningIds)
            _runningApps.Add(id);
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
        var shouldShow = _behavior.ShouldShowDock(_settings.Current, PointerNearEdge, _runningApps.Count > 0);
        if (shouldShow)
        {
            _hideAfterUtc = null;
            IsVisible = true;
            return;
        }

        if (_settings.Current.DockDisplayMode is DockDisplayMode.Normal or DockDisplayMode.AlwaysShow)
        {
            _hideAfterUtc = null;
            IsVisible = false;
            return;
        }

        _hideAfterUtc = DateTimeOffset.UtcNow.AddMilliseconds(Math.Max(0, _settings.Current.HideDockDelayMs));
    }

    public void ApplyHideDelayIfDue()
    {
        if (_hideAfterUtc is null || DateTimeOffset.UtcNow < _hideAfterUtc) return;
        _hideAfterUtc = null;
        IsVisible = false;
    }

    private async Task EnsureIconsAsync()
    {
        foreach (var item in _effectiveItems)
        {
            if (item.Kind is DockItemKind.Application or DockItemKind.File)
            {
                try { await _iconPipeline.ProcessAsync(item.TargetPath).ConfigureAwait(false); }
                catch { /* icon extraction can fail for protected paths */ }
            }
        }

        foreach (var assetId in new[] { "icon-finder", "icon-launchpad", "icon-calendar", "icon-trash", "icon-weather", "icon-preferences" })
        {
            var catalog = new AssetCatalogService();
            var path = catalog.ResolvePath(assetId);
            SystemIconFallbackGenerator.EnsureFallback(assetId, path);
        }

        UiRefresh?.Invoke();
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
            DockBarWidth = Math.Max(400, (_effectiveItems.Count * (s.IconSize + s.IconSpace) + 48) * Appearance.DpiScale);
        }
    }

    private void RefreshLayout()
    {
        var s = _settings.Current;
        var items = _effectiveItems.Count > 0 ? _effectiveItems : s.DockItems;
        LayoutItems = _layoutEngine.ComputeLayout(new PremiumDockLayoutRequest
        {
            Items = items,
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
            Progress = _progress.GetProgress(item.Item.TargetPath),
            ShowRunningDot = _runningApps.Contains(item.Item.Id)
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
            return SystemIconFallbackGenerator.EnsureFallback(assetId, path);
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
    public PremiumDockLayoutItem? Layout { get; init; }
    public string IconCachePath { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public double LabelOpacity { get; init; }
    public int? BadgeCount { get; init; }
    public double Progress { get; init; }
    public bool ShowRunningDot { get; init; }
    public double RenderSize => Layout?.Size ?? 0;
    public double RenderX => Layout?.X ?? 0;
    public double RenderY => Layout?.Y ?? 0;
    public double LightOverlayOpacity => Layout?.LightIntensity ?? 0;
    public double SelectRingOpacity => Layout?.SelectGlow ?? 0;
    public double ReflectionOpacity => Layout?.ReflectionOpacity ?? 0;
    public double ShadowOpacity => Layout?.ShadowOpacity ?? 0;
}
