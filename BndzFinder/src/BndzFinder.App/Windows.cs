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
using BndzFinder.Preferences.ViewModels;
using BndzFinder.StageManager.Controls;
using BndzFinder.StageManager.ViewModels;
using BndzFinder.Theming.Customization;
using BndzFinder.Theming.Glass;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace BndzFinder.App;

public partial class App : Application
{
    private IHost? _host;

    public App() => InitializeComponent();

    public static IServiceProvider Services =>
        ((App)Current)._host?.Services
        ?? throw new InvalidOperationException("Application host not initialized.");

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(ConfigureServices)
            .Build();

        var orchestrator = Services.GetRequiredService<IShellOrchestrator>();
        await orchestrator.StartAsync();

        var settings = Services.GetRequiredService<ISettingsService>();

        new DockWindow().Activate();

        if (settings.Current.FinderEnabled)
            new FinderWindow().Activate();

        if (settings.Current.LaunchpadEnabled)
            new LaunchpadWindow().Activate();

        if (settings.Current.StageManagerEnabled)
            new StageManagerWindow().Activate();

        var taskbar = Services.GetRequiredService<ITaskbarController>();
        if (settings.Current.HideTaskbarWhenDockShown)
            taskbar.SetAutoHide(true);
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
        services.AddSingleton<ISystemMetricsService, WmiSystemMetricsService>();
        services.AddSingleton<ITrayMirrorFacade, TrayMirrorFacade>();
        services.AddSingleton<DockViewModel>();
        services.AddSingleton<FinderViewModel>();
        services.AddSingleton<LaunchpadViewModel>();
        services.AddSingleton<StageManagerViewModel>();
        services.AddSingleton<PreferencesViewModel>();
    }
}

public sealed class DockWindow : Window
{
    public DockWindow()
    {
        Title = "Bndz-Finder Dock";
        var dockVm = App.Services.GetRequiredService<DockViewModel>();
        Content = new DockBarControl { ViewModel = dockVm };

        ConfigureChrome();
        ApplyBackdrop(dockVm.Appearance?.Glass);

        Activated += OnActivated;
    }

    private void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated) return;
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
        var height = (int)App.Services.GetRequiredService<DockViewModel>().DockBarHeight;
        appBar.Register(hwnd, edge, height);
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
            GlassEffectKind.Mica => new Microsoft.UI.Xaml.Media.MicaBackdrop(),
            GlassEffectKind.Acrylic or GlassEffectKind.LiquidGlass =>
                new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop(),
            _ => null
        } ?? new Microsoft.UI.Xaml.Media.MicaBackdrop();
    }
}

public sealed class FinderWindow : Window
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
        SystemBackdrop = new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop();
    }
}

public sealed class LaunchpadWindow : Window
{
    public LaunchpadWindow()
    {
        Title = "Bndz-Finder Launchpad";
        var vm = App.Services.GetRequiredService<LaunchpadViewModel>();
        Content = new LaunchpadControl { ViewModel = vm };

        if (AppWindow.Presenter is OverlappedPresenter p)
        {
            p.Maximize();
            p.SetBorderAndTitleBar(true, true);
        }
        SystemBackdrop = new Microsoft.UI.Xaml.Media.AcrylicBackdrop();
    }
}

public sealed class StageManagerWindow : Window
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
        Title = "Bndz-Finder Preferences";
        var vm = App.Services.GetRequiredService<PreferencesViewModel>();
        Content = new PreferencesShellControl { ViewModel = vm };

        if (AppWindow.Presenter is OverlappedPresenter p)
        {
            p.SetBorderAndTitleBar(true, true);
            p.IsResizable = true;
        }
        SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
    }
}
