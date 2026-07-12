using BndzFinder.Animations;
using BndzFinder.Core.Ipc;
using BndzFinder.Core.Models;
using BndzFinder.Core.Orchestration;
using BndzFinder.Core.Services;
using BndzFinder.Interop;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BndzFinder.ShellHost;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddBndzFinderCore();
                services.AddSingleton<IShellHostServer, NamedPipeShellHostServer>();
                services.AddSingleton<GlobalHotkeyService>();
                services.AddSingleton<WinEventHookService>();
                services.AddSingleton<TrayIconMirrorService>();
                services.AddSingleton<MinimizeAnimatorFactory>();
                services.AddSingleton<MinimizeAnimationEngine>();
                services.AddSingleton<IWindowCaptureService, WindowCaptureService>();
                services.AddSingleton<ITaskbarProgressService, TaskbarProgressService>();
                services.AddSingleton<IMinimizeHookService, MinimizeHookService>();
                services.AddSingleton<IHotkeyBindingService, HotkeyBindingService>();
                services.AddSingleton<IHotkeyBindingRegistrar>(sp => (IHotkeyBindingRegistrar)sp.GetRequiredService<IHotkeyBindingService>());
                services.AddSingleton<IHotkeySyncService, HotkeySyncService>();
                services.AddSingleton<IHotCornerMonitor, HotCornerMonitor>();
                services.AddSingleton<IKeyboardInterceptService, KeyboardInterceptService>();
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
    private readonly IShellHostServer _server;
    private readonly TrayIconMirrorService _trayMirror;
    private readonly MinimizeAnimationEngine _animator;
    private readonly IWindowCaptureService _capture;
    private readonly ITaskbarProgressService _progressMirror;
    private readonly IMinimizeHookService _minimizeHook;
    private readonly GlobalHotkeyService _globalHotkeys;
    private readonly WinEventHookService _winEvents;
    private readonly IHotkeyBindingRegistrar _hotkeys;
    private readonly IHotkeySyncService _hotkeySync;
    private readonly IHotCornerMonitor _hotCorners;
    private readonly IKeyboardInterceptService _keyboard;
    private ShellHostHotkeyRegistrar? _hotkeyRegistrar;
    private CancellationToken _workerToken;

    public ShellHostWorker(
        ILogger<ShellHostWorker> logger,
        ISettingsService settings,
        IShellHostServer server,
        TrayIconMirrorService trayMirror,
        MinimizeAnimationEngine animator,
        IWindowCaptureService capture,
        ITaskbarProgressService progressMirror,
        IMinimizeHookService minimizeHook,
        GlobalHotkeyService globalHotkeys,
        WinEventHookService winEvents,
        IHotkeyBindingRegistrar hotkeys,
        IHotkeySyncService hotkeySync,
        IHotCornerMonitor hotCorners,
        IKeyboardInterceptService keyboard)
    {
        _logger = logger;
        _settings = settings;
        _server = server;
        _trayMirror = trayMirror;
        _animator = animator;
        _capture = capture;
        _progressMirror = progressMirror;
        _minimizeHook = minimizeHook;
        _globalHotkeys = globalHotkeys;
        _winEvents = winEvents;
        _hotkeys = hotkeys;
        _hotkeySync = hotkeySync;
        _hotCorners = hotCorners;
        _keyboard = keyboard;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BndzFinder.ShellHost starting.");
        await _settings.LoadAsync(stoppingToken).ConfigureAwait(false);
        _workerToken = stoppingToken;
        _server.MessageReceived += OnMessageReceived;
        _ = _server.RunAsync(stoppingToken);

        _globalHotkeys.EnsureStarted();
        _winEvents.Start();
        _winEvents.WindowCreated += (_, _) => _ = BroadcastWindowListAsync(stoppingToken);
        _winEvents.WindowDestroyed += (_, _) => _ = BroadcastWindowListAsync(stoppingToken);
        _winEvents.WindowMinimized += (_, _) => _ = BroadcastWindowListAsync(stoppingToken);

        _minimizeHook.Start(OnWindowMinimized);
        _hotkeyRegistrar = new ShellHostHotkeyRegistrar(_hotkeys, _server, _logger);
        ApplyRuntimeSettings();
        _keyboard.Start(vk => { _ = OnKeyboardMinimizeAsync(stoppingToken); });

        await BroadcastTrayIconsAsync(stoppingToken).ConfigureAwait(false);
        await BroadcastWindowListAsync(stoppingToken).ConfigureAwait(false);

        while (!stoppingToken.IsCancellationRequested)
        {
            await BroadcastTrayIconsAsync(stoppingToken).ConfigureAwait(false);
            await BroadcastProgressAsync(stoppingToken).ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ConfigureAwait(false);
        }

        _minimizeHook.Stop();
        _hotCorners.Stop();
        _keyboard.Stop();
        _winEvents.Dispose();
        _globalHotkeys.Dispose();
    }

    private void ApplyRuntimeSettings()
    {
        if (_hotkeyRegistrar is null) return;
        _hotCorners.Stop();
        _hotCorners.Start(_settings.Current.HotCorners, (action, entered) =>
        {
            if (entered)
                _ = _server.BroadcastAsync(new ShellHostMessage
                {
                    Type = ShellHostMessageType.HotkeyPressed,
                    Payload = $"corner:{action}"
                }, _workerToken);
        });
        _hotkeySync.ApplyFromSettings(_settings.Current, _hotkeyRegistrar);
    }

    private async Task BroadcastTrayIconsAsync(CancellationToken ct)
    {
        var icons = _trayMirror.GetVisibleTrayIcons();
        var payload = JsonSerializer.Serialize(icons.Select(i => new
        {
            i.Tooltip,
            i.IconId,
            OwnerWindow = i.OwnerWindow.ToInt64(),
            IconDataBase64 = i.IconData is { Length: > 0 } data ? Convert.ToBase64String(data) : null
        }));
        await _server.BroadcastAsync(new ShellHostMessage
        {
            Type = ShellHostMessageType.TrayIconsUpdated,
            Payload = payload
        }, ct).ConfigureAwait(false);
    }

    private async Task BroadcastProgressAsync(CancellationToken ct)
    {
        var merged = new Dictionary<string, ProgressEntrySnapshot>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in _progressMirror.GetActiveProgress())
        {
            merged[entry.ExePath] = new ProgressEntrySnapshot
            {
                ExePath = entry.ExePath,
                Value = entry.Value
            };
        }

        foreach (var icon in _trayMirror.GetVisibleTrayIcons())
        {
            var tooltipProgress = TaskbarProgressService.ParsePercent(icon.Tooltip);
            if (tooltipProgress is null) continue;
            if (!TryGetExeFromOwner(icon.OwnerWindow, out var exePath)) continue;
            if (!merged.TryGetValue(exePath, out var existing) || tooltipProgress.Value > existing.Value)
            {
                merged[exePath] = new ProgressEntrySnapshot
                {
                    ExePath = exePath,
                    Value = tooltipProgress.Value
                };
            }
        }

        await _server.BroadcastAsync(new ShellHostMessage
        {
            Type = ShellHostMessageType.ProgressUpdated,
            Payload = ProgressPayload.Serialize(merged.Values)
        }, ct).ConfigureAwait(false);
    }

    private static bool TryGetExeFromOwner(nint hwnd, out string exePath)
    {
        exePath = string.Empty;
        if (hwnd == nint.Zero) return false;
        try
        {
            _ = GetWindowThreadProcessId(hwnd, out var pid);
            if (pid == 0) return false;
            using var process = System.Diagnostics.Process.GetProcessById((int)pid);
            exePath = process.MainModule?.FileName ?? string.Empty;
            return !string.IsNullOrWhiteSpace(exePath);
        }
        catch
        {
            return false;
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

    private async Task BroadcastWindowListAsync(CancellationToken ct)
    {
        var windows = WindowEnumerationService.GetOpenWindows(_settings.Current.StageManagerBlacklist);
        await _server.BroadcastAsync(new ShellHostMessage
        {
            Type = ShellHostMessageType.WindowListUpdated,
            Payload = JsonSerializer.Serialize(windows.Select(w => new { w.Title, Handle = w.Hwnd.ToInt64() }))
        }, ct).ConfigureAwait(false);
    }

    private void OnMessageReceived(object? sender, ShellHostMessage message)
    {
        switch (message.Type)
        {
            case ShellHostMessageType.RestoreRequested:
                _logger.LogDebug("Restore requested for {Hwnd}", message.WindowHandle);
                _ = _server.BroadcastAsync(new ShellHostMessage
                {
                    Type = ShellHostMessageType.RestoreCompleted,
                    WindowHandle = message.WindowHandle
                });
                break;
            case ShellHostMessageType.Shutdown:
                _logger.LogInformation("Shutdown requested.");
                break;
            case ShellHostMessageType.SettingsReload:
                _ = ReloadSettingsAsync();
                break;
        }
    }

    private async Task ReloadSettingsAsync()
    {
        await _settings.LoadAsync().ConfigureAwait(false);
        ApplyRuntimeSettings();
        _logger.LogDebug("ShellHost settings reloaded from {Path}", _settings.SettingsPath);
    }

    private void OnWindowMinimized(nint hwnd) =>
        _ = AnimateMinimizeAsync(hwnd);

    private async Task OnKeyboardMinimizeAsync(CancellationToken ct)
    {
        _logger.LogDebug("Win+Down minimize intercepted.");
        await Task.CompletedTask;
    }

    private async Task AnimateMinimizeAsync(nint hwnd)
    {
        var capture = _capture.CaptureWindow(hwnd);
        _capture.GetWindowRect(hwnd, out var rect);
        var screenHeight = GetSystemMetrics(1);
        var screenWidth = GetSystemMetrics(0);
        var targetX = screenWidth / 2.0;
        var targetY = screenHeight - 72;
        var request = new MinimizeAnimationRequest
        {
            SourceWindow = hwnd,
            WindowSnapshot = capture?.Pixels ?? [],
            SnapshotWidth = capture?.Width ?? 0,
            SnapshotHeight = capture?.Height ?? 0,
            TargetX = targetX,
            TargetY = targetY,
            TargetWidth = 48,
            TargetHeight = 48,
            DurationSeconds = 0.35 / Math.Max(0.25, _settings.Current.MinimizeAnimationSpeed)
        };

        await _server.BroadcastAsync(new ShellHostMessage
        {
            Type = ShellHostMessageType.MinimizeStarted,
            WindowHandle = hwnd.ToInt64(),
            Payload = MinimizeStartedPayload.Serialize(
                hwnd.ToInt64(),
                rect.Left,
                rect.Top,
                rect.Right - rect.Left,
                rect.Bottom - rect.Top,
                _settings.Current.MinimizeEffect.ToString(),
                snapshotBase64: capture is null
                    ? null
                    : BgraPngEncoder.EncodePngBase64(capture.Pixels, capture.Width, capture.Height),
                targetX: targetX,
                targetY: targetY)
        }, CancellationToken.None).ConfigureAwait(false);

        await _animator.AnimateAsync(request, _settings.Current.MinimizeEffect, CancellationToken.None).ConfigureAwait(false);
        await _server.BroadcastAsync(new ShellHostMessage
        {
            Type = ShellHostMessageType.MinimizeCompleted,
            WindowHandle = hwnd.ToInt64()
        }).ConfigureAwait(false);
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);
}

internal sealed class ShellHostHotkeyRegistrar : IHotkeyBindingRegistrar
{
    private readonly IHotkeyBindingRegistrar _inner;
    private readonly IShellHostServer _server;
    private readonly ILogger _logger;

    public ShellHostHotkeyRegistrar(IHotkeyBindingRegistrar inner, IShellHostServer server, ILogger logger)
    {
        _inner = inner;
        _server = server;
        _logger = logger;
    }

    public void Register(HotkeyBinding binding, Action handler) =>
        _inner.Register(binding, () =>
        {
            _logger.LogDebug("Hotkey {Id} pressed", binding.Id);
            _ = _server.BroadcastAsync(new ShellHostMessage
            {
                Type = ShellHostMessageType.HotkeyPressed,
                Payload = binding.Id
            });
            handler();
        });

    public void Unregister(string bindingId) => _inner.Unregister(bindingId);
}
