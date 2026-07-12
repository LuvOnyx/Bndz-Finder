using System.Runtime.InteropServices;

namespace BndzFinder.Interop;

public enum AppBarEdge
{
    Left = 0,
    Top = 1,
    Right = 2,
    Bottom = 3
}

public enum AppBarMessage
{
    New = 0,
    Remove = 1,
    QueryPos = 2,
    SetPos = 3,
    GetState = 4,
    GetTaskbarPos = 5,
    Activate = 6,
    GetAutoHideBar = 7,
    SetAutoHideBar = 8
}

[StructLayout(LayoutKind.Sequential)]
public struct APPBARDATA
{
    public uint cbSize;
    public nint hWnd;
    public uint uCallbackMessage;
    public uint uEdge;
    public RECT rc;
    public int lParam;
}

public interface IAppBarService
{
    RECT Register(nint hwnd, AppBarEdge edge, int widthOrHeight);
    void Unregister(nint hwnd);
    RECT QueryPosition(nint hwnd, AppBarEdge edge, int size);
}

public sealed class AppBarService : IAppBarService
{
    private const uint AbmNew = 0;
    private const uint AbmRemove = 1;
    private const uint AbmQueryPos = 2;
    private const uint AbmSetPos = 3;

    public RECT Register(nint hwnd, AppBarEdge edge, int widthOrHeight)
    {
        if (!OperatingSystem.IsWindows()) return default;

        var bar = CreateBarData(hwnd, edge);
        bar.rc = QueryPosition(hwnd, edge, widthOrHeight);
        _ = SHAppBarMessage(AbmSetPos, ref bar);
        return bar.rc;
    }

    public void Unregister(nint hwnd)
    {
        if (!OperatingSystem.IsWindows()) return;
        var bar = new APPBARDATA { cbSize = (uint)Marshal.SizeOf<APPBARDATA>(), hWnd = hwnd };
        _ = SHAppBarMessage(AbmRemove, ref bar);
    }

    public RECT QueryPosition(nint hwnd, AppBarEdge edge, int size)
    {
        if (!OperatingSystem.IsWindows()) return default;

        var bar = CreateBarData(hwnd, edge);
        _ = SHAppBarMessage(AbmNew, ref bar);
        _ = SHAppBarMessage(AbmQueryPos, ref bar);

        switch (edge)
        {
            case AppBarEdge.Bottom:
                bar.rc.Top = bar.rc.Bottom - size;
                break;
            case AppBarEdge.Top:
                bar.rc.Bottom = bar.rc.Top + size;
                break;
            case AppBarEdge.Left:
                bar.rc.Right = bar.rc.Left + size;
                break;
            case AppBarEdge.Right:
                bar.rc.Left = bar.rc.Right - size;
                break;
        }

        return bar.rc;
    }

    private static APPBARDATA CreateBarData(nint hwnd, AppBarEdge edge) => new()
    {
        cbSize = (uint)Marshal.SizeOf<APPBARDATA>(),
        hWnd = hwnd,
        uEdge = (uint)edge
    };

    [DllImport("shell32.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern uint SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);
}

public interface ITaskbarController
{
    void SetAutoHide(bool enabled);
    void Hide(bool allMonitors = false);
    void Show(bool allMonitors = false);
}

public sealed class TaskbarController : ITaskbarController
{
    private const string PrimaryTaskbarClass = "Shell_TrayWnd";
    private const string SecondaryTaskbarClass = "Shell_SecondaryTrayWnd";
    private const int SwHide = 0;
    private const int SwShow = 5;

    public void SetAutoHide(bool enabled)
    {
        if (!OperatingSystem.IsWindows()) return;
        foreach (var hwnd in EnumerateTaskbars(allMonitors: true))
        {
            var bar = new APPBARDATA
            {
                cbSize = (uint)Marshal.SizeOf<APPBARDATA>(),
                hWnd = hwnd,
                lParam = enabled ? 1 : 0
            };
            _ = SHAppBarMessage(8, ref bar);
        }
    }

    public void Hide(bool allMonitors = false)
    {
        if (!OperatingSystem.IsWindows()) return;
        SetAutoHide(false);
        foreach (var hwnd in EnumerateTaskbars(allMonitors))
            _ = ShowWindow(hwnd, SwHide);
    }

    public void Show(bool allMonitors = false)
    {
        if (!OperatingSystem.IsWindows()) return;
        foreach (var hwnd in EnumerateTaskbars(allMonitors))
            _ = ShowWindow(hwnd, SwShow);
        SetAutoHide(false);
    }

    private static IEnumerable<nint> EnumerateTaskbars(bool allMonitors)
    {
        var handles = new List<nint>();
        var primary = FindWindow(PrimaryTaskbarClass, null);
        if (primary != nint.Zero)
            handles.Add(primary);

        if (allMonitors)
        {
            EnumWindows((hwnd, _) =>
            {
                var cls = GetClassName(hwnd);
                if (cls == SecondaryTaskbarClass)
                    handles.Add(hwnd);
                return true;
            }, nint.Zero);
        }

        return handles;
    }

    private static string GetClassName(nint hwnd)
    {
        var buffer = new char[256];
        _ = GetClassName(hwnd, buffer, buffer.Length);
        return new string(buffer).TrimEnd('\0');
    }

    private delegate bool EnumWindowsProc(nint hwnd, nint lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(nint hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(nint hWnd, char[] lpClassName, int nMaxCount);

    [DllImport("shell32.dll")]
    private static extern uint SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);
}

public interface IWindowPreviewService
{
    nint RegisterThumbnail(nint destinationHwnd, nint sourceHwnd);
    void UpdateThumbnail(nint thumbnail, int x, int y, int width, int height, byte opacity);
    void UnregisterThumbnail(nint thumbnail);
}

public sealed class WindowPreviewService : IWindowPreviewService
{
    public nint RegisterThumbnail(nint destinationHwnd, nint sourceHwnd)
    {
        if (!OperatingSystem.IsWindows()) return nint.Zero;
        nint thumb = 0;
        _ = DwmRegisterThumbnail(destinationHwnd, sourceHwnd, ref thumb);
        return thumb;
    }

    public void UpdateThumbnail(nint thumbnail, int x, int y, int width, int height, byte opacity)
    {
        if (!OperatingSystem.IsWindows() || thumbnail == nint.Zero) return;
        var props = new DWM_THUMBNAIL_PROPERTIES
        {
            dwFlags = 0x1 | 0x2 | 0x4 | 0x8,
            rcDestination = new RECT { Left = x, Top = y, Right = x + width, Bottom = y + height },
            opacity = opacity,
            fVisible = true
        };
        _ = DwmUpdateThumbnailProperties(thumbnail, ref props);
    }

    public void UnregisterThumbnail(nint thumbnail)
    {
        if (!OperatingSystem.IsWindows() || thumbnail == nint.Zero) return;
        _ = DwmUnregisterThumbnail(thumbnail);
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmRegisterThumbnail(nint dest, nint src, ref nint thumb);

    [DllImport("dwmapi.dll")]
    private static extern int DwmUnregisterThumbnail(nint thumb);

    [DllImport("dwmapi.dll")]
    private static extern int DwmUpdateThumbnailProperties(nint hThumb, ref DWM_THUMBNAIL_PROPERTIES props);

    [StructLayout(LayoutKind.Sequential)]
    private struct DWM_THUMBNAIL_PROPERTIES
    {
        public uint dwFlags;
        public RECT rcDestination;
        public RECT rcSource;
        public byte opacity;
        public bool fVisible;
        public bool fSourceClientAreaOnly;
    }
}
