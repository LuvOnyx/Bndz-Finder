using BndzFinder.Core.Models;

namespace BndzFinder.Core.Services;

public interface INotificationService
{
    bool SystemNotificationsEnabled { get; set; }
    void ShowUsbConnected(string deviceName);
    void ShowUsbDisconnected(string deviceName);
    void ShowBluetoothEvent(string deviceName, string eventType);
    void ShowAppNotification(string title, string body, TimeSpan duration);
}

public sealed class NotificationService : INotificationService
{
    public bool SystemNotificationsEnabled { get; set; } = true;

    public void ShowUsbConnected(string deviceName) => _ = deviceName;
    public void ShowUsbDisconnected(string deviceName) => _ = deviceName;
    public void ShowBluetoothEvent(string deviceName, string eventType) { _ = deviceName; _ = eventType; }
    public void ShowAppNotification(string title, string body, TimeSpan duration) { _ = title; _ = body; _ = duration; }
}

public interface IGlobalHotkeyRegistry
{
    void Bind(HotkeyBinding binding, Action action);
    void Unbind(string id);
}

public sealed class GlobalHotkeyRegistry : IGlobalHotkeyRegistry
{
    private readonly Dictionary<string, Action> _bindings = new();

    public void Bind(HotkeyBinding binding, Action action) => _bindings[binding.Id] = action;
    public void Unbind(string id) => _bindings.Remove(id);
}
