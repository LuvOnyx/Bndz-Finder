using BndzFinder.Animations;
using BndzFinder.Core.Models;
using BndzFinder.Core.Orchestration;
using BndzFinder.Core.Services;
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
                services.AddBndzFinderCore();
                services.AddSingleton<GlobalHotkeyService>();
                services.AddSingleton<WinEventHookService>();
                services.AddSingleton<TrayIconMirrorService>();
                services.AddSingleton<MinimizeAnimatorFactory>();
                services.AddSingleton<IMinimizeHookService, MinimizeHookService>();
                services.AddSingleton<IHotkeyBindingService, HotkeyBindingService>();
                services.AddSingleton<IHotkeyBindingRegistrar>(sp => sp.GetRequiredService<IHotkeyBindingService>() as IHotkeyBindingRegistrar
                    ?? throw new InvalidOperationException("Hotkey binding registrar unavailable."));
                services.AddSingleton<IHotkeySyncService, HotkeySyncService>();
                services.AddSingleton<IHotCornerMonitor, HotCornerMonitor>();
                services.AddHostedService<ShellHostWorker>();
            })
            .Build();

        await host.RunAsync().ConfigureAwait(false);
    }
}

internal sealed class ShellHostWorker : BackgroundService
{
    private readonly ILogger<ShellHostWorker> _logger;
    private readonly ISettingsService _settings;
    private readonly TrayIconMirrorService _trayMirror;
    private readonly MinimizeAnimatorFactory _animators;
    private readonly IMinimizeHookService _minimizeHook;
    private readonly IHotkeyBindingRegistrar _hotkeys;
    private readonly IHotkeySyncService _hotkeySync;
    private readonly IHotCornerMonitor _hotCorners;

    public ShellHostWorker(
        ILogger<ShellHostWorker> logger,
        ISettingsService settings,
        TrayIconMirrorService trayMirror,
        MinimizeAnimatorFactory animators,
        IMinimizeHookService minimizeHook,
        IHotkeyBindingRegistrar hotkeys,
        IHotkeySyncService hotkeySync,
        IHotCornerMonitor hotCorners)
    {
        _logger = logger;
        _settings = settings;
        _trayMirror = trayMirror;
        _animators = animators;
        _minimizeHook = minimizeHook;
        _hotkeys = hotkeys;
        _hotkeySync = hotkeySync;
        _hotCorners = hotCorners;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BndzFinder.ShellHost starting (Dockmod equivalent).");
        await _settings.LoadAsync(stoppingToken).ConfigureAwait(false);

        _minimizeHook.Start(OnWindowMinimized);
        _hotkeySync.ApplyFromSettings(_settings.Current, _hotkeys);
        _hotCorners.Start((action, entered) =>
        {
            if (!entered) return;
            _logger.LogDebug("Hot corner triggered: {Action}", action);
        });

        while (!stoppingToken.IsCancellationRequested)
        {
            var icons = _trayMirror.GetVisibleTrayIcons();
            if (icons.Count > 0)
                _logger.LogDebug("Tray mirror: {Count} icons", icons.Count);
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ConfigureAwait(false);
        }

        _minimizeHook.Stop();
        _hotCorners.Stop();
    }

    private void OnWindowMinimized(nint hwnd)
    {
        _logger.LogDebug("Minimize intercepted for HWND {Hwnd}", hwnd);
        var effect = _settings.Current.MinimizeEffect;
        var animator = _animators.Get(effect);
        _ = animator.AnimateAsync(new MinimizeAnimationRequest
        {
            SourceWindow = hwnd,
            WindowSnapshot = [],
            SnapshotWidth = 0,
            SnapshotHeight = 0,
            TargetX = 0, TargetY = 0,
            TargetWidth = 48, TargetHeight = 48
        });
    }
}
