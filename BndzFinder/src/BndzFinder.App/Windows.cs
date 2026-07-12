using BndzFinder.Core.Orchestration;
using BndzFinder.Core.Services;
using BndzFinder.Dock.Controls;
using BndzFinder.Dock.Services;
using BndzFinder.Dock.ViewModels;
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

    public App()
    {
        InitializeComponent();
    }

    public static IServiceProvider Services =>
        ((App)Current)._host?.Services
        ?? throw new InvalidOperationException("Application host not initialized.");

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddBndzFinderCore();
                services.AddSingleton<IGlassBackdropService, GlassBackdropService>();
                services.AddSingleton<IAccentColorService, AccentColorService>();
                services.AddSingleton<IThemeResolver, ThemeResolver>();
                services.AddSingleton<IDockAppearanceService, DockAppearanceService>();
                services.AddSingleton<DockViewModel>();
            })
            .Build();

        var orchestrator = _host.Services.GetRequiredService<IShellOrchestrator>();
        await orchestrator.StartAsync();

        var dockWindow = new DockWindow();
        dockWindow.Activate();

        if (_host.Services.GetRequiredService<ISettingsService>().Current.FinderEnabled)
        {
            var finderWindow = new FinderWindow();
            finderWindow.Activate();
        }
    }
}

public sealed class DockWindow : Window
{
    public DockWindow()
    {
        Title = "Bndz-Finder Dock";

        var dockVm = App.Services.GetRequiredService<DockViewModel>();
        var dockBar = new DockBarControl { ViewModel = dockVm };
        Content = dockBar;

        var presenter = AppWindow.Presenter as OverlappedPresenter;
        if (presenter is not null)
        {
            presenter.IsResizable = false;
            presenter.SetBorderAndTitleBar(false, false);
            presenter.IsAlwaysOnTop = true;
        }

        AppWindow.IsShownInSwitchers = false;
        ApplyBackdrop(dockVm.Appearance?.Glass);

        var hwnd = WindowNative.GetWindowHandle(this);
        _ = hwnd;
    }

    private void ApplyBackdrop(GlassConfiguration? glass)
    {
        if (glass is null)
        {
            SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
            return;
        }

        SystemBackdrop = glass.Effect switch
        {
            GlassEffectKind.Mica => new Microsoft.UI.Xaml.Media.MicaBackdrop(),
            GlassEffectKind.Acrylic or GlassEffectKind.LiquidGlass =>
                new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop(),
            _ => null
        };
    }
}

public sealed class FinderWindow : Window
{
    public FinderWindow()
    {
        Title = "Bndz-Finder Finder";
        Content = new Microsoft.UI.Xaml.Controls.Grid { Height = 28 };
        SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
        AppWindow.IsShownInSwitchers = false;
    }
}
