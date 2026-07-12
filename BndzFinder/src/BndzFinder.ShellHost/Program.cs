using BndzFinder.Animations;
using BndzFinder.Core.Ipc;
using BndzFinder.Core.Models;
using BndzFinder.Interop;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BndzFinder.ShellHost;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddSingleton<GlobalHotkeyService>();
                services.AddSingleton<WinEventHookService>();
                services.AddSingleton<TrayIconMirrorService>();
                services.AddSingleton<MinimizeAnimatorFactory>();
                services.AddHostedService<ShellHostWorker>();
            })
            .Build();

        await host.RunAsync().ConfigureAwait(false);
    }
}

internal sealed class ShellHostWorker : BackgroundService
{
    private readonly ILogger<ShellHostWorker> _logger;
    private readonly GlobalHotkeyService _hotkeys;
    private readonly WinEventHookService _winEvents;
    private readonly TrayIconMirrorService _trayMirror;
    private readonly MinimizeAnimatorFactory _animators;

    public ShellHostWorker(
        ILogger<ShellHostWorker> logger,
        GlobalHotkeyService hotkeys,
        WinEventHookService winEvents,
        TrayIconMirrorService trayMirror,
        MinimizeAnimatorFactory animators)
    {
        _logger = logger;
        _hotkeys = hotkeys;
        _winEvents = winEvents;
        _trayMirror = trayMirror;
        _animators = animators;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BndzFinder.ShellHost starting (Dockmod equivalent).");
        _winEvents.WindowMinimized += OnWindowMinimized;
        _winEvents.Start();

        while (!stoppingToken.IsCancellationRequested)
        {
            var icons = _trayMirror.GetVisibleTrayIcons();
            if (icons.Count > 0)
            {
                _logger.LogDebug("Tray mirror: {Count} icons", icons.Count);
            }
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ConfigureAwait(false);
        }
    }

    private void OnWindowMinimized(object? sender, nint hwnd)
    {
        _logger.LogDebug("Minimize intercepted for HWND {Hwnd}", hwnd);
        var animator = _animators.Get(MinimizeEffect.Genie);
        _ = animator.AnimateAsync(new MinimizeAnimationRequest
        {
            SourceWindow = hwnd,
            WindowSnapshot = [],
            SnapshotWidth = 0,
            SnapshotHeight = 0,
            TargetX = 0,
            TargetY = 0,
            TargetWidth = 48,
            TargetHeight = 48
        });
    }
}
