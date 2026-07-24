using BndzFinder.Core.Models;
using BndzFinder.Core.Orchestration;
using BndzFinder.Core.Services;
using BndzFinder.Dock.Controls;
using BndzFinder.Dock.Services;
using BndzFinder.Dock.ViewModels;
using BndzFinder.Finder.Controls;
using BndzFinder.Finder.ViewModels;
using BndzFinder.Interop;
using BndzFinder.Launchpad.Controls;
using BndzFinder.Launchpad.ViewModels;
using BndzFinder.Preferences.Controls;
using BndzFinder.Preferences.Localization;
using BndzFinder.Preferences.ViewModels;
using BndzFinder.StageManager.Controls;
using BndzFinder.StageManager.ViewModels;
using BndzFinder.Shell.Assets;
using BndzFinder.Shell.Icons;
using BndzFinder.Shell.Services;
using BndzFinder.Theming;
using BndzFinder.Theming.Customization;
using BndzFinder.Theming.Glass;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using WinRT.Interop;

namespace BndzFinder.App;

public partial class App : Application
{
    private IHost? _host;
    private DockWindow? _dockWindow;
    private FinderWindow? _finderWindow;
    private LaunchpadWindow? _launchpadWindow;
    private StageManagerWindow? _stageWindow;
    private PreferencesWindow? _prefsWindow;
    private ScreenRoundManager? _screenRound;
    private MinimizeOverlayWindow? _minimizeOverlay;

    public App()
    {
        InitializeComponent();
        UnhandledException += (_, e) =>
        {
            e.Handled = true;
            StartupErrorReporter.Report(e.Exception, "WinUI");
        };
    }

    public static IServiceProvider Services =>
        ((App)Current)._host?.Services
        ?? throw new InvalidOperationException("Application host not initialized.");

    public static Window? MainWindow { get; private set; }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            // WinUI 3 has no Application.OnExit — restore taskbar on dispatcher shutdown.
            DispatcherQueue.GetForCurrentThread().ShutdownCompleted += (_, _) =>
            {
                _screenRound?.Dispose();
                _host?.Services.GetService<ITaskbarLifecycleService>()?.Restore();
            };

            _host = Host.CreateDefaultBuilder()
                .ConfigureServices(ConfigureServices)
                .Build();

            var settings = Services.GetRequiredService<ISettingsService>();
            var overlays = Services.GetRequiredService<IShellOverlayController>();
            var dockVm = Services.GetRequiredService<DockViewModel>();

            Services.GetRequiredService<IMacBrandingBootstrap>().EnsureBrandingAssets();
            Services.GetRequiredService<IMacBrandingBootstrap>().ApplyFigmaTokens(settings.Current);

            var appearance = Services.GetRequiredService<IMacAppearanceService>();
            appearance.ApplyFromSettings(settings.Current);

            var taskbarLifecycle = Services.GetRequiredService<ITaskbarLifecycleService>();
            taskbarLifecycle.EnableReplacementMode();

            // Show the dock BEFORE any await — WinUI exits if no window exists during async startup.
            _dockWindow = new DockWindow();
            _dockWindow.Activate();
            MainWindow = _dockWindow;
            UiHostContext.GetOwnerWindowHandle = () =>
                MainWindow is not null ? WindowNative.GetWindowHandle(MainWindow) : nint.Zero;
            _dockWindow.InitializePlacement();
            _dockWindow.ApplyVisibility(true);
            taskbarLifecycle.SyncWithDock(true);

            var orchestrator = Services.GetRequiredService<IShellOrchestrator>();
            await orchestrator.StartAsync().ConfigureAwait(true);

            Loc.Language = settings.Current.Language;
            dockVm.RefreshAll();

            var finderVm = Services.GetRequiredService<FinderViewModel>();
            var launchpadVm = Services.GetRequiredService<LaunchpadViewModel>();
            var stageVm = Services.GetRequiredService<StageManagerViewModel>();

            overlays.DockVisibilityChanged += () =>
            {
                _dockWindow?.ApplyVisibility(overlays.IsDockVisible);
                SyncTaskbar();
            };
            overlays.FinderVisibilityChanged += () => _finderWindow?.ApplyVisibility(overlays.IsFinderVisible);
            overlays.LaunchpadVisibilityChanged += () => UpdateLaunchpad(overlays.IsLaunchpadVisible);
            overlays.StageManagerVisibilityChanged += () => _stageWindow?.ApplyVisibility(overlays.IsStageManagerVisible);
            overlays.PreferencesRequested += () => ShowPreferences();

            var bridge = Services.GetRequiredService<IShellBridgeService>();
            _minimizeOverlay = new MinimizeOverlayWindow();
            bridge.HotkeyPressed += (_, id) => overlays.HandleHotkey(id);
            bridge.MinimizeStarted += (_, json) =>
            {
                var info = MinimizeStartedPayload.Deserialize(json);
                if (info is null) return;

                if (settings.Current.MinimizeIntoAppIcon)
                {
                    var target = _dockWindow?.ResolveMinimizeTarget(info.Handle);
                    if (target is { } t)
                    {
                        info = new MinimizeStartedInfo
                        {
                            Handle = info.Handle,
                            X = info.X,
                            Y = info.Y,
                            Width = info.Width,
                            Height = info.Height,
                            Effect = info.Effect,
                            SnapshotBase64 = info.SnapshotBase64,
                            TargetX = t.X,
                            TargetY = t.Y
                        };
                    }
                }

                _minimizeOverlay?.Play(info);
            };
            bridge.RestoreRequested += (_, hwnd) => _dockWindow?.RestoreWindow(hwnd);
            bridge.TrayIconsUpdated += (_, json) => finderVm.ApplyTrayIconsFromPayload(json);
            bridge.WindowListUpdated += (_, json) => stageVm.ApplyWindowsFromPayload(json);
            bridge.ProgressUpdated += (_, json) => dockVm.ApplyProgressFromPayload(json);

            ApplyScreenRound(settings);

            settings.SettingsChanged += (_, _) =>
            {
                Loc.Language = settings.Current.Language;
                dockVm.RefreshAll();
                finderVm.NotifyWidgetVisibility();
                SyncTaskbar();
                ApplyScreenRound(settings);
                Services.GetRequiredService<IMacAppearanceService>().ApplyFromSettings(settings.Current);
                _ = bridge.NotifySettingsReloadAsync();
            };

            dockVm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName is nameof(DockViewModel.IsVisible))
                    SyncTaskbar();
            };

            void SyncTaskbar() =>
                Services.GetRequiredService<ITaskbarLifecycleService>()
                    .SyncWithDock(overlays.IsDockVisible && dockVm.IsVisible);

            _dockWindow.ApplyVisibility(overlays.IsDockVisible && dockVm.IsVisible);
            SyncTaskbar();

            StartupErrorReporter.ReportMessage(
                $"Dock visible={overlays.IsDockVisible && dockVm.IsVisible}, " +
                $"displayMode={settings.Current.DockDisplayMode}, " +
                $"dockPosition={settings.Current.DockPosition}",
                "startup");

            if (settings.Current.FinderEnabled)
            {
                _finderWindow = new FinderWindow();
                _finderWindow.InitializePlacement();
                overlays.SetFinderVisible(true);
                _finderWindow.ApplyVisibility(true);
            }

            _launchpadWindow = new LaunchpadWindow();
            _launchpadWindow.ApplyVisibility(false);

            if (settings.Current.StageManagerEnabled)
            {
                _stageWindow = new StageManagerWindow();
                _stageWindow.ApplyVisibility(false);
            }

            StartupErrorReporter.ReportMessage("Bndz-Finder started successfully.", "startup");
        }
        catch (Exception ex)
        {
            _host?.Services.GetService<ITaskbarLifecycleService>()?.Restore();
            StartupErrorReporter.Report(ex);
        }
    }

    private void UpdateLaunchpad(bool visible)
    {
        if (_launchpadWindow is null) return;
        var vm = Services.GetRequiredService<LaunchpadViewModel>();
        if (!visible)
        {
            vm.SearchQuery = string.Empty;
            vm.IsOverlayVisible = false;
        }
        else
        {
            vm.IsOverlayVisible = true;
        }

        _launchpadWindow.ApplyVisibility(visible);
        if (visible) _launchpadWindow.Activate();
    }

    private void ShowPreferences()
    {
        _prefsWindow ??= new PreferencesWindow();
        UiHostContext.GetOwnerWindowHandle = () => WindowNative.GetWindowHandle(_prefsWindow);
        _prefsWindow.Activate();
    }

    private void ApplyScreenRound(ISettingsService settings)
    {
        _screenRound ??= new ScreenRoundManager();
        var monitors = Services.GetRequiredService<IDisplayMonitorService>().GetMonitors();
        _screenRound.Apply(
            monitors,
            settings.Current.ScreenRoundEnabled,
            settings.Current.ScreenRoundRadius,
            settings.Current.ScreenRoundColor);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddBndzFinderCore();
        services.AddSingleton<IGlassBackdropService, GlassBackdropService>();
        services.AddSingleton<IAccentColorService, AccentColorService>();
        services.AddSingleton<IThemeResolver, ThemeResolver>();
        services.AddSingleton<IDockAppearanceService, DockAppearanceService>();
        services.AddSingleton<IAppBarService, AppBarService>();
        services.AddSingleton<ITaskbarController, TaskbarController>();
        services.AddSingleton<ITaskbarLifecycleService>(sp => new TaskbarLifecycleService(
            sp.GetRequiredService<ITaskbarController>(),
            () => sp.GetRequiredService<ISettingsService>().Current.HideTaskbarWhenDockShown,
            () => sp.GetRequiredService<ISettingsService>().Current.HideTaskbarAllMonitors,
            () => sp.GetRequiredService<ISettingsService>().Current.AutoHideTaskbarAtStartup));
        services.AddSingleton<IMacBrandingBootstrap, MacBrandingBootstrap>();
        services.AddSingleton<IAssetCatalogService, AssetCatalogService>();
        services.AddSingleton<IFigmaAssetService, FigmaAssetService>();
        services.AddSingleton<IWallpaperService, WindowsWallpaperService>();
        services.AddSingleton<IWallpaperApplicator, WindowsWallpaperApplicator>();
        services.AddSingleton<IUiFontApplicator, WinUiFontApplicator>();
        services.AddSingleton<IMacAppearanceService, MacAppearanceService>();
        services.AddSingleton<IThemePackResolver, ThemePackResolver>();
        services.AddSingleton<IThemePackService, ThemePackService>();
        services.AddSingleton<IWindowPreviewService, WindowPreviewService>();
        services.AddSingleton<IWindowCaptureService, WindowCaptureService>();
        services.AddSingleton<WeatherService>();
        services.AddSingleton<TrayIconMirrorService>();
        services.AddSingleton<ITrayMirrorFacade, TrayMirrorFacade>();
        services.AddSingleton<IIconPipeline>(sp =>
        {
            var packs = sp.GetRequiredService<IThemePackResolver>();
            var settings = sp.GetRequiredService<ISettingsService>();
            return new MacStyleIconPipeline(appIconShellPath: packs.ResolveIconShellPath(settings.Current));
        });
        services.AddSingleton<ISystemMetricsService, WindowsSystemMetricsService>();
        services.AddSingleton<IForegroundAppService, ForegroundAppService>();
        services.AddSingleton<DockViewModel>(sp => new DockViewModel(
            sp.GetRequiredService<ISettingsService>(),
            iconPipeline: sp.GetRequiredService<IIconPipeline>(),
            overlays: sp.GetRequiredService<IShellOverlayController>(),
            capture: sp.GetRequiredService<IWindowCaptureService>(),
            windowPreview: sp.GetRequiredService<IWindowPreviewService>(),
            themePacks: sp.GetRequiredService<IThemePackResolver>(),
            figma: sp.GetRequiredService<IFigmaAssetService>()));
        services.AddSingleton<FinderViewModel>(sp =>
        {
            var settings = sp.GetRequiredService<ISettingsService>();
            var vm = new FinderViewModel(
                settings,
                sp.GetRequiredService<ISystemMetricsService>(),
                sp.GetRequiredService<ITrayMirrorFacade>(),
                sp.GetRequiredService<WeatherService>(),
                sp.GetRequiredService<IThemePackResolver>(),
                sp.GetRequiredService<IForegroundAppService>(),
                sp.GetRequiredService<IShellOverlayController>());
            vm.PreferencesRequested += () => sp.GetRequiredService<IShellOverlayController>().ShowPreferences();
            return vm;
        });
        services.AddSingleton<LaunchpadViewModel>(sp => new LaunchpadViewModel(
            sp.GetRequiredService<ISettingsService>(),
            overlays: sp.GetRequiredService<IShellOverlayController>(),
            iconPipeline: sp.GetRequiredService<IIconPipeline>()));
        services.AddSingleton<StageManagerViewModel>(sp => new StageManagerViewModel(
            sp.GetRequiredService<ISettingsService>(),
            sp.GetRequiredService<IWindowCaptureService>()));
        services.AddSingleton<PreferencesViewModel>(sp => new PreferencesViewModel(
            sp.GetRequiredService<ISettingsService>(),
            sp.GetRequiredService<IThemePackService>(),
            sp.GetRequiredService<IThemePackResolver>(),
            sp.GetRequiredService<IMacAppearanceService>(),
            sp.GetRequiredService<IBackupService>(),
            sp.GetRequiredService<IDisplayMonitorService>()));
    }
}

public abstract class ShellOverlayWindow : Window
{
    public void ApplyVisibility(bool visible)
    {
        if (AppWindow.Presenter is OverlappedPresenter presenter)
            presenter.IsAlwaysOnTop = visible;

        if (visible)
            AppWindow.Show();
        else
            AppWindow.Hide();
    }
}

public sealed class DockWindow : ShellOverlayWindow
{
    private readonly DockViewModel _dockVm;
    private readonly DockBarControl _dockBar;
    private readonly ActivationBarWindow _activationBar = new();
    private readonly DispatcherTimer _edgeTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private bool _appBarRegistered;

    public DockWindow()
    {
        Title = "Bndz-Finder Dock";
        _dockVm = App.Services.GetRequiredService<DockViewModel>();
        _dockVm.UiRefresh = () => Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread()
            .TryEnqueue(_dockVm.RefreshLayout);
        _dockBar = new DockBarControl { ViewModel = _dockVm };
        Content = _dockBar;
        ConfigureChrome();
        ApplyBackdrop(_dockVm.Appearance?.Glass);
        ResizeToDockMetrics();
        Activated += OnActivated;
        _dockVm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(DockViewModel.DockBarHeight)
                or nameof(DockViewModel.DockBarWidth)
                or nameof(DockViewModel.Appearance)
                or nameof(DockViewModel.IsVisible))
            {
                if (args.PropertyName is nameof(DockViewModel.Appearance))
                    ApplyBackdrop(_dockVm.Appearance?.Glass);
                if (args.PropertyName is nameof(DockViewModel.DockBarHeight)
                    or nameof(DockViewModel.DockBarWidth))
                    ResizeToDockMetrics();
                if (_appBarRegistered)
                    PositionOnAppBar();
                ApplyVisibility(_dockVm.IsVisible);
            }
        };
        _edgeTimer.Tick += (_, _) =>
        {
            UpdateEdgeActivation(_dockVm);
            _dockVm.ApplyHideDelayIfDue();
        };
        _edgeTimer.Start();
    }

    public void InitializePlacement()
    {
        _appBarRegistered = true;
        PositionOnAppBar();
        ApplyVisibility(true);
    }

    public (double X, double Y)? ResolveMinimizeTarget(long hwnd)
    {
        var itemId = _dockVm.FindItemIdForWindow(hwnd);
        if (string.IsNullOrWhiteSpace(itemId)) return null;
        return _dockBar.GetIconScreenCenter(itemId);
    }

    public void RestoreWindow(long hwnd) => WindowOperations.FocusWindow((nint)hwnd);

    private void ResizeToDockMetrics()
    {
        // Window spans full monitor width via AppBar; only pre-size height before registration.
        var height = (int)Math.Clamp(_dockVm.DockBarHeight + 32, 72, 240);
        if (_appBarRegistered)
            return;
        AppWindow.Resize(new SizeInt32(800, height));
    }

    private void UpdateEdgeActivation(DockViewModel dockVm)
    {
        if (!OperatingSystem.IsWindows()) return;
        var settings = App.Services.GetRequiredService<ISettingsService>().Current;
        if (!GetCursorPos(out var point)) return;

        var screenWidth = GetSystemMetrics(0);
        var screenHeight = GetSystemMetrics(1);
        var threshold = Math.Max(4, settings.ActivationBarHeight);

        dockVm.PointerNearEdge = settings.DockPosition switch
        {
            Core.Models.DockPosition.Left => point.X <= threshold,
            Core.Models.DockPosition.Right => point.X >= screenWidth - threshold,
            Core.Models.DockPosition.Top => point.Y <= threshold,
            _ => point.Y >= screenHeight - threshold
        };

        _activationBar.Update(settings, dockVm.PointerNearEdge, screenWidth, screenHeight);
    }

    private void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated) return;
        _appBarRegistered = true;
        PositionOnAppBar();
    }

    private void PositionOnAppBar()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var appBar = App.Services.GetRequiredService<IAppBarService>();
        var settings = App.Services.GetRequiredService<ISettingsService>();
        var edge = settings.Current.DockPosition switch
        {
            Core.Models.DockPosition.Left => AppBarEdge.Left,
            Core.Models.DockPosition.Right => AppBarEdge.Right,
            Core.Models.DockPosition.Top => AppBarEdge.Top,
            _ => AppBarEdge.Bottom
        };
        var size = Math.Max(48, (int)_dockVm.DockBarHeight + settings.Current.EdgePosition);
        var rect = appBar.Register(hwnd, edge, size);
        if (rect.Right > rect.Left && rect.Bottom > rect.Top)
        {
            AppWindow.MoveAndResize(new RectInt32(
                rect.Left,
                rect.Top,
                rect.Right - rect.Left,
                rect.Bottom - rect.Top));
        }
    }

    private void ConfigureChrome()
    {
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.SetBorderAndTitleBar(false, false);
            presenter.IsAlwaysOnTop = true;
        }
        AppWindow.IsShownInSwitchers = false;
    }

    private void ApplyBackdrop(GlassConfiguration? glass)
    {
        SystemBackdrop = glass?.Effect switch
        {
            GlassEffectKind.Mica => new MicaBackdrop(),
            GlassEffectKind.Acrylic or GlassEffectKind.LiquidGlass => new DesktopAcrylicBackdrop(),
            _ => new MicaBackdrop()
        };
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct Point { public int X; public int Y; }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool GetCursorPos(out Point lpPoint);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);
}

public sealed class FinderWindow : ShellOverlayWindow
{
    private bool _appBarRegistered;

    public FinderWindow()
    {
        Title = "Bndz-Finder Finder";
        var vm = App.Services.GetRequiredService<FinderViewModel>();
        var stageVm = App.Services.GetRequiredService<StageManagerViewModel>();
        Content = new FinderBarControl { ViewModel = vm, StageManagerViewModel = stageVm };
        if (AppWindow.Presenter is OverlappedPresenter p)
        {
            p.IsResizable = false;
            p.SetBorderAndTitleBar(false, false);
            p.IsAlwaysOnTop = true;
        }
        AppWindow.IsShownInSwitchers = false;
        SystemBackdrop = new DesktopAcrylicBackdrop();
        Activated += (_, args) =>
        {
            if (args.WindowActivationState != WindowActivationState.Deactivated)
                PositionOnAppBar();
        };
    }

    public void InitializePlacement()
    {
        _appBarRegistered = true;
        PositionOnAppBar();
        ApplyVisibility(true);
    }

    private void PositionOnAppBar()
    {
        if (!_appBarRegistered && AppWindow is null) return;
        _appBarRegistered = true;
        var hwnd = WindowNative.GetWindowHandle(this);
        var appBar = App.Services.GetRequiredService<IAppBarService>();
        var settings = App.Services.GetRequiredService<ISettingsService>();
        var size = Math.Max(28, settings.Current.FinderHeight + settings.Current.FinderOffsetY);
        var rect = appBar.Register(hwnd, AppBarEdge.Top, size);
        if (rect.Right > rect.Left && rect.Bottom > rect.Top)
        {
            AppWindow.MoveAndResize(new RectInt32(
                rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top));
        }
    }
}

public sealed class LaunchpadWindow : ShellOverlayWindow
{
    public LaunchpadWindow()
    {
        Title = "Bndz-Finder Launchpad";
        var vm = App.Services.GetRequiredService<LaunchpadViewModel>();
        Content = new LaunchpadControl { ViewModel = vm };
        if (AppWindow.Presenter is OverlappedPresenter p)
        {
            p.Maximize();
            p.SetBorderAndTitleBar(false, false);
            p.IsAlwaysOnTop = true;
        }
        SystemBackdrop = new DesktopAcrylicBackdrop();
        if (Content is LaunchpadControl launchpad)
        {
            launchpad.KeyDown += (_, e) =>
            {
                if (e.Key == Windows.System.VirtualKey.Escape)
                    App.Services.GetRequiredService<IShellOverlayController>().HideLaunchpad();
            };
        }
    }
}

public sealed class StageManagerWindow : ShellOverlayWindow
{
    public StageManagerWindow()
    {
        Title = "Bndz-Finder Stage Manager";
        var vm = App.Services.GetRequiredService<StageManagerViewModel>();
        Content = new StageManagerStrip { ViewModel = vm };
        if (AppWindow.Presenter is OverlappedPresenter p)
        {
            p.IsResizable = false;
            p.SetBorderAndTitleBar(false, false);
            p.IsAlwaysOnTop = true;
        }
        AppWindow.IsShownInSwitchers = false;
    }
}

public sealed class PreferencesWindow : Window
{
    public PreferencesWindow()
    {
        Title = Loc.Get("Preferences.Title");
        var vm = App.Services.GetRequiredService<PreferencesViewModel>();
        Content = new PreferencesShellControl { ViewModel = vm };
        if (AppWindow.Presenter is OverlappedPresenter p)
        {
            p.SetBorderAndTitleBar(true, true);
            p.IsResizable = true;
        }
        SystemBackdrop = new MicaBackdrop();
    }
}
