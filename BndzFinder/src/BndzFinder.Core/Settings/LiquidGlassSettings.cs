namespace BndzFinder.Core.Settings;

public sealed class LiquidGlassSettings
{
    public double Distortion { get; set; } = 0.35;
    public double Refraction { get; set; } = 0.22;
    public double EdgeHighlight { get; set; } = 0.45;
    public double Saturation { get; set; } = 1.25;
    public double NoiseScale { get; set; } = 1.8;
}
