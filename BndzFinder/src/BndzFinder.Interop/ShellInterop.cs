using System.Runtime.InteropServices;

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

public sealed class GlobalHotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const string WindowClassName = "BndzFinder.HotkeyHost";
    private static readonly nint HwndMessage = new(-3);

    private readonly Dictionary<int, Action> _handlers = new();
    private readonly object _sync = new();
    private readonly ManualResetEventSlim _ready = new(false);
    private Thread? _pumpThread;
    private WndProcDelegate? _wndProc;
    private nint _hwnd;
    private bool _disposed;

    public void EnsureStarted()
    {
        if (!OperatingSystem.IsWindows() || _disposed) return;

        lock (_sync)
        {
            if (_pumpThread is not null) return;
            _pumpThread = new Thread(MessagePump)
            {
                IsBackground = true,
                Name = "BndzFinder.HotkeyPump"
            };
            _pumpThread.SetApartmentState(ApartmentState.STA);
            _pumpThread.Start();
        }

        _ready.Wait(TimeSpan.FromSeconds(5));
    }

    public bool TryRegister(int id, uint modifiers, uint virtualKey, Action handler)
    {
        if (!OperatingSystem.IsWindows() || virtualKey == 0) return false;

        EnsureStarted();
        _handlers[id] = handler;
        if (_hwnd == nint.Zero) return false;
        return RegisterHotKey(_hwnd, id, modifiers, virtualKey);
    }

    public void Unregister(int id)
    {
        if (_hwnd != nint.Zero)
            UnregisterHotKey(_hwnd, id);
        _handlers.Remove(id);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_hwnd != nint.Zero)
        {
            foreach (var id in _handlers.Keys.ToList())
                UnregisterHotKey(_hwnd, id);
            PostMessage(_hwnd, WmClose, nint.Zero, nint.Zero);
        }

        _pumpThread?.Join(TimeSpan.FromSeconds(2));
        _ready.Dispose();
    }

    private void MessagePump()
    {
        if (!OperatingSystem.IsWindows()) return;

        _wndProc = WndProc;
        var hInstance = GetModuleHandle(null);
        var atom = RegisterClass(hInstance);
        if (atom == 0 && Marshal.GetLastWin32Error() != 1410) // ERROR_CLASS_ALREADY_EXISTS
        {
            _ready.Set();
            return;
        }

        _hwnd = CreateWindowEx(
            0,
            WindowClassName,
            "BndzFinder Hotkeys",
            0,
            0,
            0,
            0,
            0,
            HwndMessage,
            nint.Zero,
            hInstance,
            nint.Zero);

        _ready.Set();
        if (_hwnd == nint.Zero) return;

        while (!_disposed)
        {
            if (!GetMessage(out var msg, nint.Zero, 0, 0))
                break;
            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
        }

        if (_hwnd != nint.Zero)
        {
            DestroyWindow(_hwnd);
            _hwnd = nint.Zero;
        }
    }

    private ushort RegisterClass(nint hInstance)
    {
        var wc = new WNDCLASS
        {
            lpfnWndProc = _wndProc!,
            hInstance = hInstance,
            lpszClassName = WindowClassName
        };
        return RegisterClassW(ref wc);
    }

    private nint WndProc(nint hwnd, uint msg, nint wParam, nint lParam)
    {
        if (msg == WmHotkey && _handlers.TryGetValue((int)wParam, out var handler))
        {
            handler();
            return nint.Zero;
        }

        if (msg == WmClose)
        {
            DestroyWindow(hwnd);
            return nint.Zero;
        }

        if (msg == WmDestroy)
        {
            PostQuitMessage(0);
            return nint.Zero;
        }

        return DefWindowProc(hwnd, msg, wParam, lParam);
    }

    private const uint WmClose = 0x0010;
    private const uint WmDestroy = 0x0002;

    private delegate nint WndProcDelegate(nint hwnd, uint msg, nint wParam, nint lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASS
    {
        public uint style;
        public WndProcDelegate lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public nint hInstance;
        public nint hIcon;
        public nint hCursor;
        public nint hbrBackground;
        public string? lpszMenuName;
        public string lpszClassName;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public nint hwnd;
        public uint message;
        public nint wParam;
        public nint lParam;
        public uint time;
        public int pt_x;
        public int pt_y;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClassW(ref WNDCLASS lpWndClass);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowEx(
        uint dwExStyle,
        string lpClassName,
        string? lpWindowName,
        uint dwStyle,
        int x,
        int y,
        int nWidth,
        int nHeight,
        nint hWndParent,
        nint hMenu,
        nint hInstance,
        nint lpParam);

    [DllImport("user32.dll")]
    private static extern nint DefWindowProc(nint hWnd, uint msg, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool GetMessage(out MSG lpMsg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern nint DispatchMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern void PostQuitMessage(int nExitCode);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(nint hWnd, uint msg, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(nint hWnd, int id);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern nint GetModuleHandle(string? lpModuleName);
}

public sealed class WinEventHookService : IDisposable
{
    private const uint EventObjectCreate = 0x8000;
    private const uint EventObjectDestroy = 0x8001;
    private const uint EventSystemMinimizeStart = 0x0016;

    private nint _createHook;
    private nint _destroyHook;
    private nint _minimizeHook;
    private WinEventDelegate? _proc;

    public event EventHandler<nint>? WindowCreated;
    public event EventHandler<nint>? WindowDestroyed;
    public event EventHandler<nint>? WindowMinimized;

    public void Start()
    {
        if (!OperatingSystem.IsWindows()) return;

        _proc = OnWinEvent;
        _createHook = SetWinEventHook(EventObjectCreate, EventObjectCreate, nint.Zero, _proc, 0, 0, 0);
        _destroyHook = SetWinEventHook(EventObjectDestroy, EventObjectDestroy, nint.Zero, _proc, 0, 0, 0);
        _minimizeHook = SetWinEventHook(EventSystemMinimizeStart, EventSystemMinimizeStart, nint.Zero, _proc, 0, 0, 0);
    }

    public void Dispose()
    {
        if (_createHook != nint.Zero)
        {
            UnhookWinEvent(_createHook);
            _createHook = nint.Zero;
        }

        if (_destroyHook != nint.Zero)
        {
            UnhookWinEvent(_destroyHook);
            _destroyHook = nint.Zero;
        }

        if (_minimizeHook != nint.Zero)
        {
            UnhookWinEvent(_minimizeHook);
            _minimizeHook = nint.Zero;
        }
    }

    private void OnWinEvent(
        nint hWinEventHook,
        uint eventType,
        nint hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime)
    {
        _ = hWinEventHook;
        _ = idObject;
        _ = idChild;
        _ = dwEventThread;
        _ = dwmsEventTime;

        if (hwnd == nint.Zero) return;

        switch (eventType)
        {
            case EventObjectCreate:
                WindowCreated?.Invoke(this, hwnd);
                break;
            case EventObjectDestroy:
                WindowDestroyed?.Invoke(this, hwnd);
                break;
            case EventSystemMinimizeStart:
                WindowMinimized?.Invoke(this, hwnd);
                break;
        }
    }

    private delegate void WinEventDelegate(
        nint hWinEventHook,
        uint eventType,
        nint hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime);

    [DllImport("user32.dll")]
    private static extern nint SetWinEventHook(
        uint eventMin,
        uint eventMax,
        nint hmodWinEventProc,
        WinEventDelegate lpfnWinEventProc,
        uint idProcess,
        uint idThread,
        uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool UnhookWinEvent(nint hWinEventHook);
}

public sealed class TrayIconMirrorService
{
    private const int TbPressButton = 0x0403;

    public IReadOnlyList<TrayIconInfo> GetVisibleTrayIcons()
    {
        if (!OperatingSystem.IsWindows()) return [];
        return EnumerateTrayToolbar();
    }

    public void ForwardClick(nint ownerWindow, uint iconId)
    {
        if (!OperatingSystem.IsWindows() || ownerWindow == nint.Zero) return;
        SendMessage(ownerWindow, TbPressButton, (nint)iconId, new nint(1));
    }

    private static List<TrayIconInfo> EnumerateTrayToolbar()
    {
        var results = new List<TrayIconInfo>();
        var trayWnd = FindWindow("Shell_TrayWnd", null);
        if (trayWnd == nint.Zero) return results;

        var notifyWnd = FindWindowEx(trayWnd, nint.Zero, "TrayNotifyWnd", null);
        if (notifyWnd == nint.Zero) return results;

        var sysPager = FindWindowEx(notifyWnd, nint.Zero, "SysPager", null);
        var toolbarParent = sysPager != nint.Zero ? sysPager : notifyWnd;
        var toolbar = FindWindowEx(toolbarParent, nint.Zero, "ToolbarWindow32", null);
        if (toolbar == nint.Zero) return results;

        var count = SendMessage(toolbar, 0x0418, nint.Zero, nint.Zero); // TB_BUTTONCOUNT
        for (var i = 0; i < (int)count; i++)
        {
            results.Add(new TrayIconInfo
            {
                Tooltip = $"Tray icon {i}",
                OwnerWindow = toolbar,
                IconId = (uint)i
            });
        }
        return results;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint FindWindow(string? cls, string? wnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint FindWindowEx(nint parent, nint childAfter, string? cls, string? wnd);

    [DllImport("user32.dll")]
    private static extern nint SendMessage(nint hWnd, int msg, nint wParam, nint lParam);
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
