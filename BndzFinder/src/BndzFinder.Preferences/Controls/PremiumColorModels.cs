namespace BndzFinder.Preferences.Controls;

/// <summary>
/// Cross-platform color model for premium picker (WinUI control binds to this).
/// </summary>
public sealed class PremiumColor
{
    public byte A { get; init; } = 255;
    public byte R { get; init; }
    public byte G { get; init; }
    public byte B { get; init; }

    public string ToHex() => $"#{R:X2}{G:X2}{B:X2}";

    public static PremiumColor FromHex(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length < 6) return new PremiumColor { R = 0, G = 120, B = 212 };
        return new PremiumColor
        {
            R = Convert.ToByte(hex[..2], 16),
            G = Convert.ToByte(hex[2..4], 16),
            B = Convert.ToByte(hex[4..6], 16)
        };
    }

    public static PremiumColor FromHsv(double hue, double saturation, double value)
    {
        var c = value * saturation;
        var x = c * (1 - Math.Abs(hue / 60 % 2 - 1));
        var m = value - c;
        double r, g, b;
        if (hue < 60) { r = c; g = x; b = 0; }
        else if (hue < 120) { r = x; g = c; b = 0; }
        else if (hue < 180) { r = 0; g = c; b = x; }
        else if (hue < 240) { r = 0; g = x; b = c; }
        else if (hue < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }
        return new PremiumColor
        {
            R = (byte)((r + m) * 255),
            G = (byte)((g + m) * 255),
            B = (byte)((b + m) * 255)
        };
    }
}

public static class PremiumColorMath
{
    public static (double H, double S, double V) ToHsv(PremiumColor color)
    {
        var r = color.R / 255.0;
        var g = color.G / 255.0;
        var b = color.B / 255.0;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var delta = max - min;
        var h = 0.0;
        if (delta > 0)
        {
            if (max == r) h = 60 * (((g - b) / delta) % 6);
            else if (max == g) h = 60 * (((b - r) / delta) + 2);
            else h = 60 * (((r - g) / delta) + 4);
        }
        if (h < 0) h += 360;
        var s = max == 0 ? 0 : delta / max;
        return (h, s, max);
    }
}
