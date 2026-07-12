using BndzFinder.Core.Ipc;
using BndzFinder.Core.Models;

namespace BndzFinder.Core.Services;

public interface IShellBridgeService
{
    event EventHandler<string>? HotkeyPressed;
    event EventHandler<long>? MinimizeCompleted;
    event EventHandler<string>? MinimizeStarted;
    event EventHandler<long>? RestoreRequested;
    event EventHandler<string>? TrayIconsUpdated;
    event EventHandler<string>? WindowListUpdated;
    Task StartAsync(CancellationToken cancellationToken = default);
    Task RequestMinimizeAsync(long hwnd, CancellationToken cancellationToken = default);
    Task RequestRestoreAsync(long hwnd, CancellationToken cancellationToken = default);
    Task ShutdownHostAsync(CancellationToken cancellationToken = default);
    Task NotifySettingsReloadAsync(CancellationToken cancellationToken = default);
}

public sealed class ShellBridgeService : IShellBridgeService, IAsyncDisposable
{
    private readonly IShellHostClient _client;
    private readonly IShellHostProcessLauncher _launcher;
    private CancellationTokenSource? _receiveCts;

    public ShellBridgeService(IShellHostClient client, IShellHostProcessLauncher launcher)
    {
        _client = client;
        _launcher = launcher;
    }

    public event EventHandler<string>? HotkeyPressed;
    public event EventHandler<long>? MinimizeCompleted;
    public event EventHandler<string>? MinimizeStarted;
    public event EventHandler<long>? RestoreRequested;
    public event EventHandler<string>? TrayIconsUpdated;
    public event EventHandler<string>? WindowListUpdated;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _launcher.EnsureRunningAsync(cancellationToken).ConfigureAwait(false);
        if (!_client.IsConnected)
            await _client.ConnectAsync(cancellationToken).ConfigureAwait(false);

        _receiveCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _ = ReceiveLoopAsync(_receiveCts.Token);
        await _client.SendAsync(new ShellHostMessage { Type = ShellHostMessageType.Ping }, cancellationToken).ConfigureAwait(false);
    }

    public Task RequestMinimizeAsync(long hwnd, CancellationToken cancellationToken = default) =>
        _client.SendAsync(new ShellHostMessage { Type = ShellHostMessageType.MinimizeRequested, WindowHandle = hwnd }, cancellationToken);

    public Task RequestRestoreAsync(long hwnd, CancellationToken cancellationToken = default) =>
        _client.SendAsync(new ShellHostMessage { Type = ShellHostMessageType.RestoreRequested, WindowHandle = hwnd }, cancellationToken);

    public Task ShutdownHostAsync(CancellationToken cancellationToken = default) =>
        _client.SendAsync(new ShellHostMessage { Type = ShellHostMessageType.Shutdown }, cancellationToken);

    public Task NotifySettingsReloadAsync(CancellationToken cancellationToken = default) =>
        _client.SendAsync(new ShellHostMessage { Type = ShellHostMessageType.SettingsReload }, cancellationToken);

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        await foreach (var message in _client.ReceiveAsync(ct).ConfigureAwait(false))
        {
            switch (message.Type)
            {
                case ShellHostMessageType.HotkeyPressed:
                    HotkeyPressed?.Invoke(this, message.Payload ?? string.Empty);
                    break;
                case ShellHostMessageType.MinimizeCompleted:
                    MinimizeCompleted?.Invoke(this, message.WindowHandle);
                    break;
                case ShellHostMessageType.MinimizeStarted:
                    MinimizeStarted?.Invoke(this, message.Payload ?? string.Empty);
                    break;
                case ShellHostMessageType.RestoreRequested:
                    RestoreRequested?.Invoke(this, message.WindowHandle);
                    break;
                case ShellHostMessageType.TrayIconsUpdated:
                    TrayIconsUpdated?.Invoke(this, message.Payload ?? "[]");
                    break;
                case ShellHostMessageType.WindowListUpdated:
                    WindowListUpdated?.Invoke(this, message.Payload ?? "[]");
                    break;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _receiveCts?.Cancel();
        await _client.DisconnectAsync().ConfigureAwait(false);
        if (_client is IAsyncDisposable d) await d.DisposeAsync().ConfigureAwait(false);
    }
}

public interface IShellHostProcessLauncher
{
    Task EnsureRunningAsync(CancellationToken cancellationToken = default);
}

public sealed class ShellHostProcessLauncher : IShellHostProcessLauncher
{
    private System.Diagnostics.Process? _process;

    public Task EnsureRunningAsync(CancellationToken cancellationToken = default)
    {
        if (_process is { HasExited: false }) return Task.CompletedTask;

        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "BndzFinder.ShellHost.exe"),
            Path.Combine(AppContext.BaseDirectory, "BndzFinder.ShellHost.dll"),
            Path.Combine(AppContext.BaseDirectory, "..", "BndzFinder.ShellHost", "BndzFinder.ShellHost.exe"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
                "BndzFinder.ShellHost", "bin", "Release", "net10.0", "BndzFinder.ShellHost.dll")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
                "BndzFinder.ShellHost", "bin", "Publish", "Portable", "win-x64", "BndzFinder.ShellHost.exe"))
        };

        foreach (var path in candidates)
        {
            if (!File.Exists(path)) continue;
            _process = path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                ? System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"\"{path}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                })
                : System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            break;
        }

        return Task.CompletedTask;
    }
}

public interface IShellOverlayController
{
    bool IsDockVisible { get; }
    bool IsFinderVisible { get; }
    bool IsLaunchpadVisible { get; }
    bool IsStageManagerVisible { get; }

    event Action? DockVisibilityChanged;
    event Action? FinderVisibilityChanged;
    event Action? LaunchpadVisibilityChanged;
    event Action? StageManagerVisibilityChanged;
    event Action? PreferencesRequested;

    void ToggleDock();
    void ToggleFinder();
    void SetDockVisible(bool visible);
    void SetFinderVisible(bool visible);
    void ShowLaunchpad();
    void HideLaunchpad();
    void ToggleStageManager();
    void ShowPreferences();
    void HandleHotkey(string bindingId);
    void HandleHotCorner(HotCornerAction action);
}

public sealed class ShellOverlayController : IShellOverlayController
{
    private readonly ISettingsService _settings;
    private bool _dockVisible = true;
    private bool _finderVisible;
    private bool _launchpadVisible;
    private bool _stageVisible;

    public event Action? DockVisibilityChanged;
    public event Action? FinderVisibilityChanged;
    public event Action? LaunchpadVisibilityChanged;
    public event Action? StageManagerVisibilityChanged;
    public event Action? PreferencesRequested;

    public ShellOverlayController(ISettingsService settings) => _settings = settings;

    public bool IsDockVisible => _dockVisible;
    public bool IsFinderVisible => _finderVisible;
    public bool IsLaunchpadVisible => _launchpadVisible;
    public bool IsStageManagerVisible => _stageVisible;

    public void ToggleDock()
    {
        SetDockVisible(!_dockVisible);
    }

    public void SetDockVisible(bool visible)
    {
        if (_dockVisible == visible) return;
        _dockVisible = visible;
        DockVisibilityChanged?.Invoke();
    }

    public void SetFinderVisible(bool visible)
    {
        if (!_settings.Current.FinderEnabled) return;
        if (_finderVisible == visible) return;
        _finderVisible = visible;
        FinderVisibilityChanged?.Invoke();
    }

    public void ToggleFinder()
    {
        if (!_settings.Current.FinderEnabled) return;
        _finderVisible = !_finderVisible;
        FinderVisibilityChanged?.Invoke();
    }

    public void ShowLaunchpad()
    {
        if (!_settings.Current.LaunchpadEnabled) return;
        _launchpadVisible = true;
        LaunchpadVisibilityChanged?.Invoke();
    }

    public void HideLaunchpad()
    {
        _launchpadVisible = false;
        LaunchpadVisibilityChanged?.Invoke();
    }

    public void ToggleStageManager()
    {
        if (!_settings.Current.StageManagerEnabled) return;
        _stageVisible = !_stageVisible;
        StageManagerVisibilityChanged?.Invoke();
    }

    public void ShowPreferences() => PreferencesRequested?.Invoke();

    public void HandleHotkey(string bindingId)
    {
        if (bindingId.StartsWith("corner:", StringComparison.OrdinalIgnoreCase)
            && Enum.TryParse<HotCornerAction>(bindingId["corner:".Length..], true, out var cornerAction))
        {
            HandleHotCorner(cornerAction);
            return;
        }

        switch (bindingId)
        {
            case "hotkeyDock":
            case "dock":
                ToggleDock();
                break;
            case "hotkeyfinder":
            case "finder":
                ToggleFinder();
                break;
            case "hotkeypad":
            case "launchpad":
                ShowLaunchpad();
                break;
            case "stagemanager_hotkey":
            case "stage":
                ToggleStageManager();
                break;
            case "preferences":
                ShowPreferences();
                break;
        }
    }

    public void HandleHotCorner(HotCornerAction action)
    {
        switch (action)
        {
            case HotCornerAction.Launchpad:
                ShowLaunchpad();
                break;
            case HotCornerAction.StageManager:
                ToggleStageManager();
                break;
            case HotCornerAction.ShowDesktop:
                if (OperatingSystem.IsWindows())
                    _ = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer", "shell:::{3080F90D-D7AD-11D9-BD98-0000947B0177}") { UseShellExecute = true });
                break;
            case HotCornerAction.StartMenu:
                if (OperatingSystem.IsWindows())
                    _ = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer", "shell:::{2559a1f3-21d7-11d4-bdaf-00c04f60b9f0}") { UseShellExecute = true });
                break;
            case HotCornerAction.ActionCenter:
                if (OperatingSystem.IsWindows())
                    _ = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer", "shell:::{48ded4a8-d2dd-451d-bf6d-4d3363b7f16b}") { UseShellExecute = true });
                break;
            case HotCornerAction.LockScreen:
                if (OperatingSystem.IsWindows())
                    _ = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("rundll32.exe", "user32.dll,LockWorkStation") { UseShellExecute = true });
                break;
            case HotCornerAction.DisplaySleep:
                if (OperatingSystem.IsWindows())
                    NativeShell.SendDisplaySleep();
                break;
            case HotCornerAction.WindowsWidgets:
                if (OperatingSystem.IsWindows())
                    _ = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "ms-widgets:",
                        UseShellExecute = true
                    });
                break;
        }
    }
}

internal static class NativeShell
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern nint SendMessage(nint hWnd, int msg, nint wParam, nint lParam);

    public static void SendDisplaySleep() =>
        SendMessage(new nint(0xffff), 0x0112, new nint(0xF170), new nint(2));
}
