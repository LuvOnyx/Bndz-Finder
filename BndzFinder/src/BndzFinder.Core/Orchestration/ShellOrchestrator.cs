using BndzFinder.Core.Ipc;
using BndzFinder.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BndzFinder.Core.Orchestration;

public interface IShellOrchestrator
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}

public sealed class ShellOrchestrator : IShellOrchestrator
{
    private readonly ISettingsService _settings;
    private readonly ISingleInstanceService _singleInstance;
    private readonly IShellBridgeService _shellBridge;
    private readonly ILogger<ShellOrchestrator> _logger;

    public ShellOrchestrator(
        ISettingsService settings,
        ISingleInstanceService singleInstance,
        IShellBridgeService shellBridge,
        ILogger<ShellOrchestrator> logger)
    {
        _settings = settings;
        _singleInstance = singleInstance;
        _shellBridge = shellBridge;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (!_singleInstance.TryAcquire("BndzFinder.App"))
        {
            throw new InvalidOperationException("Another instance of Bndz-Finder is already running.");
        }

        await _settings.LoadAsync(cancellationToken).ConfigureAwait(false);
        await _shellBridge.StartAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Bndz-Finder orchestrator started. Settings loaded from {Path}", _settings.SettingsPath);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _shellBridge.ShutdownHostAsync(cancellationToken).ConfigureAwait(false);
        if (_shellBridge is IAsyncDisposable d)
            await d.DisposeAsync().ConfigureAwait(false);
        _singleInstance.Release();
        _logger.LogInformation("Bndz-Finder orchestrator stopped.");
    }
}

public static class CoreServiceCollectionExtensions
{
    public static IServiceCollection AddBndzFinderCore(this IServiceCollection services)
    {
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ISingleInstanceService, SingleInstanceService>();
        services.AddSingleton<IShellOrchestrator, ShellOrchestrator>();
        services.AddSingleton<IShellHostClient, NamedPipeShellHostClient>();
        services.AddSingleton<IShellHostProcessLauncher, ShellHostProcessLauncher>();
        services.AddSingleton<IShellBridgeService, ShellBridgeService>();
        services.AddSingleton<IShellOverlayController, ShellOverlayController>();
        services.AddSingleton<IDisplayMonitorService, DisplayMonitorService>();
        services.AddSingleton<IHotkeySyncService, HotkeySyncService>();
        services.AddSingleton<IBackupService, BackupService>();
        services.AddSingleton<IStartupService, StartupService>();
        return services;
    }
}
