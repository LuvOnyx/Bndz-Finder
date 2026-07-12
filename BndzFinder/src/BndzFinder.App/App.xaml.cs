using BndzFinder.Core.Orchestration;
using BndzFinder.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;

namespace BndzFinder.App;

public partial class App : Application
{
    private IHost? _host;

    public App()
    {
        InitializeComponent();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services => services.AddBndzFinderCore())
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
