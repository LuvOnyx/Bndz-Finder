using System.Runtime.InteropServices;
using SkiaSharp;

namespace BndzFinder.Shell.Icons;

/// <summary>
/// Extracts file/shell icons on Windows via SHGetFileInfo and encodes to PNG for the icon cache.
/// </summary>
public static class WindowsFileIconExtractor
{
    private const uint ShgfiIcon = 0x000000100;
    private const uint ShgfiLargeIcon = 0x000000000;
    private const uint ShgfiUseFileAttributes = 0x000000010;
    private const uint FileAttributeNormal = 0x00000080;

    public static bool TryExtractToPng(string filePath, string outputPngPath, int size = 256)
    {
        if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(filePath)) return false;

        var shfi = new SHFILEINFO();
        var flags = ShgfiIcon | ShgfiLargeIcon;
        if (!File.Exists(filePath))
            flags |= ShgfiUseFileAttributes;

        var result = SHGetFileInfo(
            filePath,
            FileAttributeNormal,
            ref shfi,
            (uint)Marshal.SizeOf<SHFILEINFO>(),
            flags);

        if (result == nint.Zero || shfi.hIcon == nint.Zero) return false;

        try
        {
            return TrySaveIconAsPng(shfi.hIcon, outputPngPath, size);
        }
        finally
        {
            _ = DestroyIcon(shfi.hIcon);
        }
    }

    private static bool TrySaveIconAsPng(nint hIcon, string outputPngPath, int size)
    {
        using var bitmap = SKBitmap.Decode(IconToPngBytes(hIcon, size));
        if (bitmap is null) return false;

        Directory.CreateDirectory(Path.GetDirectoryName(outputPngPath) ?? ".");
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Open(outputPngPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        data.SaveTo(stream);
        return true;
    }

    private static byte[] IconToPngBytes(nint hIcon, int size)
    {
        var info = new ICONINFO();
        if (!GetIconInfo(hIcon, ref info)) return [];

        try
        {
            if (info.hbmColor == nint.Zero) return [];

            var bmp = new BITMAP();
            _ = GetObject(info.hbmColor, Marshal.SizeOf<BITMAP>(), ref bmp);

            var width = bmp.bmWidth;
            var height = Math.Abs(bmp.bmHeight);
            if (width <= 0 || height <= 0) return [];

            using var surface = SKSurface.Create(new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            var hdc = GetDC(nint.Zero);
            var memDc = CreateCompatibleDC(hdc);
            var old = SelectObject(memDc, info.hbmColor);
            try
            {
                using var skBitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
                var infoSize = new BITMAPINFOHEADER
                {
                    biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                    biWidth = width,
                    biHeight = -height,
                    biPlanes = 1,
                    biBitCount = 32,
                    biCompression = 0
                };

                if (GetDIBits(memDc, info.hbmColor, 0, (uint)height, skBitmap.GetPixels(), ref infoSize, 0) == 0)
                    return [];

                using var image = SKImage.FromBitmap(skBitmap);
                canvas.DrawImage(image, new SKRect(0, 0, size, size));
            }
            finally
            {
                SelectObject(memDc, old);
                DeleteDC(memDc);
                ReleaseDC(nint.Zero, hdc);
            }

            using var png = surface.Snapshot().Encode(SKEncodedImageFormat.Png, 100);
            return png.ToArray();
        }
        finally
        {
            if (info.hbmColor != nint.Zero) DeleteObject(info.hbmColor);
            if (info.hbmMask != nint.Zero) DeleteObject(info.hbmMask);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEINFO
    {
        public nint hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ICONINFO
    {
        public bool fIcon;
        public int xHotspot;
        public int yHotspot;
        public nint hbmMask;
        public nint hbmColor;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAP
    {
        public int bmType;
        public int bmWidth;
        public int bmHeight;
        public int bmWidthBytes;
        public short bmPlanes;
        public short bmBitsPixel;
        public nint bmBits;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public uint biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern nint SHGetFileInfo(
        string pszPath,
        uint dwFileAttributes,
        ref SHFILEINFO psfi,
        uint cbFileInfo,
        uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(nint hIcon);

    [DllImport("user32.dll")]
    private static extern bool GetIconInfo(nint hIcon, ref ICONINFO piconinfo);

    [DllImport("gdi32.dll")]
    private static extern int GetObject(nint hgdiobj, int cbBuffer, ref BITMAP lpvObject);

    [DllImport("gdi32.dll")]
    private static extern nint SelectObject(nint hdc, nint hgdiobj);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(nint hObject);

    [DllImport("gdi32.dll")]
    private static extern nint CreateCompatibleDC(nint hdc);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(nint hdc);

    [DllImport("user32.dll")]
    private static extern nint GetDC(nint hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(nint hWnd, nint hDC);

    [DllImport("gdi32.dll")]
    private static extern int GetDIBits(
        nint hdc,
        nint hbmp,
        uint uStartScan,
        uint cScanLines,
        nint lpvBits,
        ref BITMAPINFOHEADER lpbi,
        uint uUsage);
}
