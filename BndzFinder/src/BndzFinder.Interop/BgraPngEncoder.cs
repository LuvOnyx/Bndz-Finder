using SkiaSharp;

namespace BndzFinder.Interop;

public static class BgraPngEncoder
{
    public static byte[] EncodePng(byte[] bgra, int width, int height)
    {
        if (bgra.Length == 0 || width <= 0 || height <= 0)
            return [];

        using var bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        System.Runtime.InteropServices.Marshal.Copy(bgra, 0, bitmap.GetPixels(), bgra.Length);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 90);
        return data.ToArray();
    }

    public static string EncodePngBase64(byte[] bgra, int width, int height)
    {
        var png = EncodePng(bgra, width, height);
        return png.Length == 0 ? string.Empty : Convert.ToBase64String(png);
    }
}
