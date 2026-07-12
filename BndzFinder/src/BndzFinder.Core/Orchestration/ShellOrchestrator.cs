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
    private readonly ILogger<ShellOrchestrator> _logger;

    public ShellOrchestrator(
        ISettingsService settings,
        ISingleInstanceService singleInstance,
        ILogger<ShellOrchestrator> logger)
    {
        _settings = settings;
        _singleInstance = singleInstance;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (!_singleInstance.TryAcquire("BndzFinder.App"))
        {
            throw new InvalidOperationException("Another instance of Bndz-Finder is already running.");
        }

        await _settings.LoadAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Bndz-Finder orchestrator started. Settings loaded from {Path}", _settings.SettingsPath);
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _singleInstance.Release();
        _logger.LogInformation("Bndz-Finder orchestrator stopped.");
        return Task.CompletedTask;
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
        return services;
    }
}
