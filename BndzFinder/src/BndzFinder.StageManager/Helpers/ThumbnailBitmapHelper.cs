using System.Runtime.InteropServices;
using Microsoft.UI.Xaml.Media.Imaging;

namespace BndzFinder.StageManager.Helpers;

public static class ThumbnailBitmapHelper
{
    [ComImport]
    [Guid("905A0FE0-BC53-11DF-8C49-001E4FC686E8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IBufferByteAccess
    {
        unsafe byte* Buffer();
    }

    public static WriteableBitmap? CreateFromBgra(byte[]? pixels, int width, int height, int maxSize)
    {
        if (pixels is null || width <= 0 || height <= 0 || pixels.Length < width * height * 4)
            return null;

        var scale = Math.Min(1.0, maxSize / (double)Math.Max(width, height));
        var targetWidth = Math.Max(1, (int)(width * scale));
        var targetHeight = Math.Max(1, (int)(height * scale));
        var scaled = scale < 0.999
            ? DownscaleBgra(pixels, width, height, targetWidth, targetHeight)
            : pixels;

        var byteCount = targetWidth * targetHeight * 4;
        if (scaled.Length < byteCount)
            return null;

        var bitmap = new WriteableBitmap(targetWidth, targetHeight);
        CopyPixels(bitmap, scaled, byteCount);
        return bitmap;
    }

    private static unsafe void CopyPixels(WriteableBitmap bitmap, byte[] pixels, int byteCount)
    {
        var buffer = bitmap.PixelBuffer;
        var length = (int)Math.Min(buffer.Length, (uint)byteCount);
        var access = (IBufferByteAccess)buffer;
        fixed (byte* src = pixels)
        {
            System.Buffer.MemoryCopy(src, access.Buffer(), length, length);
        }
        bitmap.Invalidate();
    }

    private static byte[] DownscaleBgra(byte[] source, int srcW, int srcH, int dstW, int dstH)
    {
        var result = new byte[dstW * dstH * 4];
        for (var y = 0; y < dstH; y++)
        {
            var srcY = y * srcH / dstH;
            for (var x = 0; x < dstW; x++)
            {
                var srcX = x * srcW / dstW;
                var srcIndex = (srcY * srcW + srcX) * 4;
                var dstIndex = (y * dstW + x) * 4;
                result[dstIndex] = source[srcIndex];
                result[dstIndex + 1] = source[srcIndex + 1];
                result[dstIndex + 2] = source[srcIndex + 2];
                result[dstIndex + 3] = source[srcIndex + 3];
            }
        }
        return result;
    }
}
