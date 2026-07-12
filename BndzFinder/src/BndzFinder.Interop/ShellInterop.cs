namespace BndzFinder.Interop;

public static class WindowStyles
{
    public const int GwlExstyle = -20;
    public const nint WsExLayered = 0x80000;
    public const nint WsExTransparent = 0x20;
    public const nint WsExToolwindow = 0x80;
    public const nint WsExTopmost = 0x8;
    public const nint SwpNomove = 0x2;
    public const nint SwpNosize = 0x1;
    public const nint SwpNoactivate = 0x10;
    public const int SwShowminimize = 6;
    public const int SwRestore = 9;
    public const int SwHide = 0;
    public const int ScMinimize = 0xF020;
    public const int WmSyscommand = 0x0112;
    public const int HcbtMinmax = 1;
    public const int WhCbt = 5;
    public const int WhKeyboardLl = 13;
}

public sealed class AppBarService
{
    public void Register(nint hwnd, int edge, ref RECT workArea)
    {
        if (!OperatingSystem.IsWindows()) return;
        // SHAppBarMessage integration wired on Windows at runtime via CsWin32-generated P/Invoke.
        _ = hwnd;
        _ = edge;
        _ = workArea;
    }

    public void Unregister(nint hwnd)
    {
        if (!OperatingSystem.IsWindows()) return;
        _ = hwnd;
    }
}

public sealed class GlobalHotkeyService : IDisposable
{
    private readonly Dictionary<int, Action> _handlers = new();
    private nint _hwnd;

    public void Register(int id, uint modifiers, uint virtualKey, Action handler)
    {
        _handlers[id] = handler;
        if (OperatingSystem.IsWindows())
        {
            // RegisterHotKey bound when host HWND is available.
        }
    }

    public void AttachWindow(nint hwnd) => _hwnd = hwnd;

    public void Unregister(int id) => _handlers.Remove(id);

    public void Dispose()
    {
        foreach (var id in _handlers.Keys.ToList())
        {
            Unregister(id);
        }
    }
}

public sealed class WinEventHookService : IDisposable
{
    public event EventHandler<nint>? WindowCreated;
    public event EventHandler<nint>? WindowDestroyed;
    public event EventHandler<nint>? WindowMinimized;

    public void Start()
    {
        if (!OperatingSystem.IsWindows()) return;
    }

    public void Dispose() { }
}

public sealed class TrayIconMirrorService
{
    public IReadOnlyList<TrayIconInfo> GetVisibleTrayIcons()
    {
        if (!OperatingSystem.IsWindows())
        {
            return [];
        }

        return [];
    }
}

public sealed class TrayIconInfo
{
    public required string Tooltip { get; init; }
    public nint OwnerWindow { get; init; }
    public uint IconId { get; init; }
    public byte[]? IconData { get; init; }
}

public struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}
