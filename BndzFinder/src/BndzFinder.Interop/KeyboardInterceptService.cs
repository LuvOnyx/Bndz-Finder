using System.Runtime.InteropServices;

namespace BndzFinder.Interop;

public interface IKeyboardInterceptService
{
    void Start(Action<int> onKeyDown);
    void Stop();
}

public sealed class KeyboardInterceptService : IKeyboardInterceptService, IDisposable
{
    private HookProc? _proc;
    private nint _hook;
    private Action<int>? _callback;

    private delegate nint HookProc(int code, nint wParam, nint lParam);

    public void Start(Action<int> onKeyDown)
    {
        if (!OperatingSystem.IsWindows()) return;
        _callback = onKeyDown;
        _proc = Callback;
        _hook = SetWindowsHookEx(WindowStyles.WhKeyboardLl, _proc, GetModuleHandle(null), 0);
    }

    public void Stop()
    {
        if (_hook != nint.Zero)
        {
            UnhookWindowsHookEx(_hook);
            _hook = nint.Zero;
        }
    }

    private nint Callback(int code, nint wParam, nint lParam)
    {
        if (code >= 0 && wParam == (nint)0x0100) // WM_KEYDOWN
        {
            var vk = Marshal.ReadInt32(lParam);
            var winDown = (GetAsyncKeyState(0x5B) & 0x8000) != 0 || (GetAsyncKeyState(0x5C) & 0x8000) != 0;
            if (winDown && vk == 0x28) // VK_DOWN
                _callback?.Invoke(vk);
        }
        return CallNextHookEx(_hook, code, wParam, lParam);
    }

    public void Dispose() => Stop();

    [DllImport("user32.dll")]
    private static extern nint SetWindowsHookEx(int idHook, HookProc lpfn, nint hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(nint hhk);

    [DllImport("user32.dll")]
    private static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern nint GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);
}
