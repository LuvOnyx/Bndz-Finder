using BndzFinder.Core.Ipc;
using BndzFinder.Core.Models;

namespace BndzFinder.Core.Services;

public interface IShellBridgeService
{
    event EventHandler<string>? HotkeyPressed;
    event EventHandler<long>? MinimizeCompleted;
    event EventHandler<long>? RestoreRequested;
    event EventHandler<string>? TrayIconsUpdated;
    event EventHandler<string>? WindowListUpdated;
    Task StartAsync(CancellationToken cancellationToken = default);
    Task RequestMinimizeAsync(long hwnd, CancellationToken cancellationToken = default);
    Task RequestRestoreAsync(long hwnd, CancellationToken cancellationToken = default);
    Task ShutdownHostAsync(CancellationToken cancellationToken = default);
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
    void ToggleDock();
    void ToggleFinder();
    void ShowLaunchpad();
    void HideLaunchpad();
    void ToggleStageManager();
    void ShowPreferences();
    void HandleHotkey(string bindingId);
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
        _dockVisible = !_dockVisible;
        DockVisibilityChanged?.Invoke();
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
        switch (bindingId)
        {
            case "hotkeyDock": ToggleDock(); break;
            case "hotkeyfinder": ToggleFinder(); break;
            case "hotkeypad": ShowLaunchpad(); break;
            case "stagemanager_hotkey": ToggleStageManager(); break;
        }
    }
}
