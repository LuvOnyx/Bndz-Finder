namespace BndzFinder.Core.Ipc;

public enum ShellHostMessageType : byte
{
    Ping = 1,
    Pong = 2,
    MinimizeRequested = 10,
    MinimizeCompleted = 11,
    MinimizeStarted = 14,
    RestoreRequested = 12,
    RestoreCompleted = 13,
    TrayIconsUpdated = 20,
    HotkeyPressed = 30,
    WindowListUpdated = 40,
    SettingsReload = 50,
    Shutdown = 255
}

public sealed class ShellHostMessage
{
    public ShellHostMessageType Type { get; init; }
    public long WindowHandle { get; init; }
    public string? Payload { get; init; }
    public long TimestampUtcTicks { get; init; } = DateTime.UtcNow.Ticks;
}

public interface IShellHostClient
{
    bool IsConnected { get; }
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task SendAsync(ShellHostMessage message, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ShellHostMessage> ReceiveAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync();
}

public interface IShellHostServer
{
    Task RunAsync(CancellationToken cancellationToken = default);
    event EventHandler<ShellHostMessage>? MessageReceived;
    Task BroadcastAsync(ShellHostMessage message, CancellationToken cancellationToken = default);
}
