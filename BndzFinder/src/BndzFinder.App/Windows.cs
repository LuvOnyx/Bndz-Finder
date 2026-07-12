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
using BndzFinder.Theming.Customization;
using BndzFinder.Theming.Glass;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

    public App() => InitializeComponent();

    public static IServiceProvider Services =>
        ((App)Current)._host?.Services
        ?? throw new InvalidOperationException("Application host not initialized.");

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices(ConfigureServices)
                .Build();

            var settings = Services.GetRequiredService<ISettingsService>();
            Loc.Language = settings.Current.Language;

            var orchestrator = Services.GetRequiredService<IShellOrchestrator>();
            await orchestrator.StartAsync();

            var overlays = Services.GetRequiredService<IShellOverlayController>();
            var dockVm = Services.GetRequiredService<DockViewModel>();
            var finderVm = Services.GetRequiredService<FinderViewModel>();
            var launchpadVm = Services.GetRequiredService<LaunchpadViewModel>();
            var stageVm = Services.GetRequiredService<StageManagerViewModel>();

            overlays.DockVisibilityChanged += () => _dockWindow?.ApplyVisibility(overlays.IsDockVisible);
            overlays.FinderVisibilityChanged += () => _finderWindow?.ApplyVisibility(overlays.IsFinderVisible);
            overlays.LaunchpadVisibilityChanged += () => UpdateLaunchpad(overlays.IsLaunchpadVisible);
            overlays.StageManagerVisibilityChanged += () => _stageWindow?.ApplyVisibility(overlays.IsStageManagerVisible);
            overlays.PreferencesRequested += () => ShowPreferences();

            var bridge = Services.GetRequiredService<IShellBridgeService>();
            bridge.HotkeyPressed += (_, id) => overlays.HandleHotkey(id);
            bridge.TrayIconsUpdated += (_, json) => finderVm.ApplyTrayIconsFromPayload(json);
            bridge.WindowListUpdated += (_, json) => stageVm.ApplyWindowsFromPayload(json);

            settings.SettingsChanged += (_, _) =>
            {
                Loc.Language = settings.Current.Language;
                dockVm.RefreshAll();
                finderVm.NotifyWidgetVisibility();
                _ = bridge.NotifySettingsReloadAsync();
            };

            _dockWindow = new DockWindow();
            _dockWindow.Activate();
            _dockWindow.ApplyVisibility(overlays.IsDockVisible && dockVm.IsVisible);

            if (settings.Current.FinderEnabled)
            {
                _finderWindow = new FinderWindow();
                _finderWindow.ApplyVisibility(false);
            }

            _launchpadWindow = new LaunchpadWindow();
            _launchpadWindow.ApplyVisibility(false);

            if (settings.Current.StageManagerEnabled)
            {
                _stageWindow = new StageManagerWindow();
                _stageWindow.ApplyVisibility(false);
            }

            var taskbar = Services.GetRequiredService<ITaskbarController>();
            if (settings.Current.HideTaskbarWhenDockShown)
                taskbar.SetAutoHide(true);

            var assets = new BndzFinder.Shell.Assets.AssetCatalogService();
            var missingIcons = assets.GetMissingSystemIcons();
            if (missingIcons.Count > 0)
            {
                var logDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "BndzFinder");
                Directory.CreateDirectory(logDir);
                var msg = $"Missing macosicons.com system icons: {string.Join(", ", missingIcons)}. "
                    + "Run: pwsh -File BndzFinder/scripts/import-macos-icons.ps1 (see ASSETS.md)";
                File.AppendAllText(Path.Combine(logDir, "startup.log"), $"[{DateTime.Now:O}] WARN {msg}{Environment.NewLine}");
            }
        }
        catch (Exception ex)
        {
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
        _prefsWindow.Activate();
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
        services.AddSingleton<IWindowPreviewService, WindowPreviewService>();
        services.AddSingleton<IWindowCaptureService, WindowCaptureService>();
        services.AddSingleton<ISystemMetricsService, WmiSystemMetricsService>();
        services.AddSingleton<TrayIconMirrorService>();
        services.AddSingleton<ITrayMirrorFacade, TrayMirrorFacade>();
        services.AddSingleton<DockViewModel>(sp => new DockViewModel(
            sp.GetRequiredService<ISettingsService>(),
            overlays: sp.GetRequiredService<IShellOverlayController>(),
            capture: sp.GetRequiredService<IWindowCaptureService>()));
        services.AddSingleton<FinderViewModel>(sp =>
        {
            var vm = new FinderViewModel(
                sp.GetRequiredService<ISettingsService>(),
                sp.GetRequiredService<ISystemMetricsService>(),
                sp.GetRequiredService<ITrayMirrorFacade>());
            vm.PreferencesRequested += () => sp.GetRequiredService<IShellOverlayController>().ShowPreferences();
            return vm;
        });
        services.AddSingleton<LaunchpadViewModel>(sp => new LaunchpadViewModel(
            sp.GetRequiredService<ISettingsService>(),
            overlays: sp.GetRequiredService<IShellOverlayController>()));
        services.AddSingleton<StageManagerViewModel>(sp => new StageManagerViewModel(
            sp.GetRequiredService<ISettingsService>(),
            sp.GetRequiredService<IWindowCaptureService>()));
        services.AddSingleton<PreferencesViewModel>();
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
    private readonly DispatcherTimer _edgeTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private bool _appBarRegistered;

    public DockWindow()
    {
        Title = "Bndz-Finder Dock";
        _dockVm = App.Services.GetRequiredService<DockViewModel>();
        Content = new DockBarControl { ViewModel = _dockVm };
        ConfigureChrome();
        ApplyBackdrop(_dockVm.Appearance?.Glass);
        Activated += OnActivated;
        _dockVm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(DockViewModel.DockBarHeight)
                or nameof(DockViewModel.Appearance)
                or nameof(DockViewModel.IsVisible))
            {
                if (args.PropertyName is nameof(DockViewModel.Appearance))
                    ApplyBackdrop(_dockVm.Appearance?.Glass);
                if (_appBarRegistered)
                    PositionOnAppBar();
                ApplyVisibility(_dockVm.IsVisible);
            }
        };
        _edgeTimer.Tick += (_, _) => UpdateEdgeActivation(_dockVm);
        _edgeTimer.Start();
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
    public FinderWindow()
    {
        Title = "Bndz-Finder Finder";
        var vm = App.Services.GetRequiredService<FinderViewModel>();
        Content = new FinderBarControl { ViewModel = vm };
        if (AppWindow.Presenter is OverlappedPresenter p)
        {
            p.IsResizable = false;
            p.SetBorderAndTitleBar(false, false);
            p.IsAlwaysOnTop = true;
        }
        AppWindow.IsShownInSwitchers = false;
        SystemBackdrop = new DesktopAcrylicBackdrop();
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

internal static class StartupErrorReporter
{
    public static void Report(Exception ex)
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BndzFinder");
        Directory.CreateDirectory(logDir);
        var logPath = Path.Combine(logDir, "startup.log");
        File.WriteAllText(logPath, $"[{DateTime.Now:O}]{Environment.NewLine}{ex}{Environment.NewLine}");

        if (OperatingSystem.IsWindows())
        {
            _ = MessageBox(
                nint.Zero,
                $"{ex.Message}{Environment.NewLine}{Environment.NewLine}Details: {logPath}",
                "Bndz-Finder could not start",
                0x10); // MB_ICONERROR
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int MessageBox(nint hWnd, string text, string caption, uint type);
}
