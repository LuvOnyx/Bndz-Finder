using System.Runtime.InteropServices;
using SkiaSharp;

namespace BndzFinder.Interop;

/// <summary>
/// Converts Win32 HICON handles to PNG bytes for tray/dock mirroring.
/// </summary>
public static class IconPngExporter
{
    private const uint DiNormal = 0x0003;

    public static byte[]? FromHIcon(nint hIcon, int size = 32)
    {
        if (hIcon == nint.Zero || !OperatingSystem.IsWindows()) return null;

        var hdc = GetDC(nint.Zero);
        if (hdc == nint.Zero) return null;

        var memDc = CreateCompatibleDC(hdc);
        if (memDc == nint.Zero)
        {
            ReleaseDC(nint.Zero, hdc);
            return null;
        }

        var hBitmap = CreateCompatibleBitmap(hdc, size, size);
        if (hBitmap == nint.Zero)
        {
            DeleteDC(memDc);
            ReleaseDC(nint.Zero, hdc);
            return null;
        }

        var old = SelectObject(memDc, hBitmap);
        try
        {
            _ = PatBlt(memDc, 0, 0, size, size, 0x00FF0062); // WHITENESS — transparent areas become white; alpha handled below
            if (!DrawIconEx(memDc, 0, 0, hIcon, size, size, 0, nint.Zero, DiNormal))
                return null;

            var bmi = new BITMAPINFOHEADER
            {
                biSize = Marshal.SizeOf<BITMAPINFOHEADER>(),
                biWidth = size,
                biHeight = -size,
                biPlanes = 1,
                biBitCount = 32,
                biCompression = 0
            };

            var pixels = new byte[size * size * 4];
            if (GetDIBits(memDc, hBitmap, 0, (uint)size, pixels, ref bmi, 0) == 0)
                return null;

            PremultiplyBgra(pixels);
            return BgraPngEncoder.EncodePng(pixels, size, size);
        }
        finally
        {
            SelectObject(memDc, old);
            DeleteObject(hBitmap);
            DeleteDC(memDc);
            ReleaseDC(nint.Zero, hdc);
        }
    }

    private static void PremultiplyBgra(byte[] pixels)
    {
        for (var i = 0; i < pixels.Length; i += 4)
        {
            var a = pixels[i + 3];
            if (a == 0)
            {
                pixels[i] = pixels[i + 1] = pixels[i + 2] = 0;
                continue;
            }

            var f = a / 255f;
            pixels[i] = (byte)(pixels[i] * f);
            pixels[i + 1] = (byte)(pixels[i + 1] * f);
            pixels[i + 2] = (byte)(pixels[i + 2] * f);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
    {
        public int biSize;
        public int biWidth;
        public int biHeight;
        public short biPlanes;
        public short biBitCount;
        public int biCompression;
        public int biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public int biClrUsed;
        public int biClrImportant;
    }

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

    [DllImport("gdi32.dll")]
    private static extern bool PatBlt(nint hdc, int x, int y, int w, int h, uint rop);

    [DllImport("user32.dll")]
    private static extern bool DrawIconEx(nint hdc, int xLeft, int yTop, nint hIcon, int cxWidth, int cyHeight, uint istepIfAniCur, nint hbrFlickerFreeDraw, uint diFlags);

    [DllImport("gdi32.dll")]
    private static extern int GetDIBits(nint hdc, nint hbmp, uint uStartScan, uint cScanLines, byte[] lpvBits, ref BITMAPINFOHEADER lpbi, uint usage);
}
