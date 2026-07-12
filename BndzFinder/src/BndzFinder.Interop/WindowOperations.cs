using System.Runtime.InteropServices;

namespace BndzFinder.Interop;

public static class WindowOperations
{
    public static void FocusWindow(nint hwnd)
    {
        if (!OperatingSystem.IsWindows() || hwnd == nint.Zero) return;
        if (IsIconic(hwnd)) ShowWindow(hwnd, 9); // SW_RESTORE
        SetForegroundWindow(hwnd);
    }

    public static void CloseWindow(nint hwnd)
    {
        if (!OperatingSystem.IsWindows() || hwnd == nint.Zero) return;
        PostMessage(hwnd, 0x0010, nint.Zero, nint.Zero); // WM_CLOSE
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(nint hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(nint hWnd, uint msg, nint wParam, nint lParam);
}
