using System.Runtime.InteropServices;

namespace BndzFinder.Interop;

public sealed class WindowCaptureResult
{
    public required byte[] Pixels { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
}

public interface IWindowCaptureService
{
    WindowCaptureResult? CaptureWindow(nint hwnd);
    bool GetWindowRect(nint hwnd, out RECT rect);
}

public sealed class WindowCaptureService : IWindowCaptureService
{
    public bool GetWindowRect(nint hwnd, out RECT rect)
    {
        rect = default;
        if (!OperatingSystem.IsWindows()) return false;
        return GetWindowRectNative(hwnd, out rect);
    }

    public WindowCaptureResult? CaptureWindow(nint hwnd)
    {
        if (!OperatingSystem.IsWindows() || hwnd == nint.Zero) return null;
        if (!GetWindowRectNative(hwnd, out var rect)) return null;

        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0) return null;

        var hdcScreen = GetDC(nint.Zero);
        var hdcMem = CreateCompatibleDC(hdcScreen);
        var hBitmap = CreateCompatibleBitmap(hdcScreen, width, height);
        var old = SelectObject(hdcMem, hBitmap);
        _ = PrintWindow(hwnd, hdcMem, 2);
        SelectObject(hdcMem, old);

        var bmp = new BITMAP();
        GetObject(hBitmap, Marshal.SizeOf<BITMAP>(), ref bmp);
        var stride = width * 4;
        var pixels = new byte[stride * height];
        var bmi = new BITMAPINFOHEADER
        {
            biSize = Marshal.SizeOf<BITMAPINFOHEADER>(),
            biWidth = width,
            biHeight = -height,
            biPlanes = 1,
            biBitCount = 32,
            biCompression = 0
        };
        GetDIBits(hdcMem, hBitmap, 0, (uint)height, pixels, ref bmi, 0);

        DeleteObject(hBitmap);
        DeleteDC(hdcMem);
        ReleaseDC(nint.Zero, hdcScreen);

        return new WindowCaptureResult { Pixels = pixels, Width = width, Height = height };
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowRect")]
    private static extern bool GetWindowRectNative(nint hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern nint GetDC(nint hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(nint hWnd, nint hDC);

    [DllImport("gdi32.dll")]
    private static extern nint CreateCompatibleDC(nint hdc);

    [DllImport("gdi32.dll")]
    private static extern nint CreateCompatibleBitmap(nint hdc, int width, int height);

    [DllImport("gdi32.dll")]
    private static extern nint SelectObject(nint hdc, nint hgdiobj);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(nint hObject);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(nint hdc);

    [DllImport("user32.dll")]
    private static extern bool PrintWindow(nint hwnd, nint hdcBlt, uint nFlags);

    [DllImport("gdi32.dll")]
    private static extern int GetObject(nint hgdiobj, int cbBuffer, ref BITMAP lpvObject);

    [DllImport("gdi32.dll")]
    private static extern int GetDIBits(nint hdc, nint hbmp, uint uStartScan, uint cScanLines,
        byte[] lpvBits, ref BITMAPINFOHEADER lpbi, uint usage);

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAP
    {
        public int bmType, bmWidth, bmHeight, bmWidthBytes;
        public ushort bmPlanes, bmBitsPixel;
        public nint bmBits;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
    {
        public int biSize, biWidth, biHeight;
        public short biPlanes, biBitCount;
        public int biCompression, biSizeImage, biXPelsPerMeter, biYPelsPerMeter, biClrUsed, biClrImportant;
    }
}
