using BndzFinder.Core.Models;

namespace BndzFinder.Shell.Dock;

public interface IPremiumDockLayoutEngine
{
    IReadOnlyList<PremiumDockLayoutItem> ComputeLayout(PremiumDockLayoutRequest request);
}

public sealed class PremiumDockLayoutRequest
{
    public required IReadOnlyList<DockItem> Items { get; init; }
    public double CursorPosition { get; init; }
    public int BaseIconSize { get; init; } = 48;
    public int MaxIconSize { get; init; } = 72;
    public int IconSpacing { get; init; } = 8;
    public IconEffectKind IconEffect { get; init; } = IconEffectKind.Scale;
    public bool IconImmersion { get; init; }
    public bool MagnificationEnabled { get; init; } = true;
    public bool ReflectionEnabled { get; init; } = true;
    public double ReflectionOpacity { get; init; } = 0.35;
    public double ReflectionBlur { get; init; } = 0.5;
    public IReadOnlySet<string>? RunningAppIds { get; init; }
    public int? HoveredIndex { get; init; }
}

public sealed class PremiumDockLayoutItem
{
    public required DockItem Item { get; init; }
    public int Index { get; init; }
    public double X { get; init; }
    public double Y { get; init; }
    public double Size { get; init; }
    public double Scale { get; init; }
    public double LightIntensity { get; init; }
    public double SelectGlow { get; init; }
    public double ImmersionTilt { get; init; }
    public double ReflectionOpacity { get; init; }
    public double ReflectionOffsetY { get; init; }
    public double ShadowOpacity { get; init; }
    public double ShadowBlur { get; init; }
    public double ShadowOffsetY { get; init; }
    public bool IsHovered { get; init; }
    public bool ShowRunningIndicator { get; init; }
    public bool IsSeparator => Item.Kind == DockItemKind.Separator;
}

public sealed class PremiumDockLayoutEngine : IPremiumDockLayoutEngine
{
    private readonly FisheyeDockLayoutEngine _fisheye = new();
    private readonly IconEffectProcessor _effects = new();

    public IReadOnlyList<PremiumDockLayoutItem> ComputeLayout(PremiumDockLayoutRequest request)
    {
        if (request.Items.Count == 0) return [];

        var baseLayout = _fisheye.ComputeLayout(
            request.Items,
            request.MagnificationEnabled ? request.CursorPosition : -9999,
            request.BaseIconSize,
            request.MaxIconSize,
            request.IconSpacing);

        var running = request.RunningAppIds ?? new HashSet<string>();
        var result = new List<PremiumDockLayoutItem>(baseLayout.Count);

        for (var i = 0; i < baseLayout.Count; i++)
        {
            var item = baseLayout[i];
            var isHovered = request.HoveredIndex == i;
            var effect = _effects.Compute(new IconEffectInput
            {
                Scale = item.Scale,
                IconEffect = request.IconEffect,
                IconImmersion = request.IconImmersion,
                IsHovered = isHovered,
                ReflectionEnabled = request.ReflectionEnabled,
                ReflectionOpacity = request.ReflectionOpacity,
                ReflectionBlur = request.ReflectionBlur
            });

            result.Add(new PremiumDockLayoutItem
            {
                Item = item.Item,
                Index = i,
                X = item.X,
                Y = item.Y + effect.VerticalLift,
                Size = item.Size,
                Scale = item.Scale,
                LightIntensity = effect.LightIntensity,
                SelectGlow = effect.SelectGlow,
                ImmersionTilt = effect.ImmersionTilt,
                ReflectionOpacity = effect.ReflectionOpacity,
                ReflectionOffsetY = effect.ReflectionOffsetY,
                ShadowOpacity = effect.ShadowOpacity,
                ShadowBlur = effect.ShadowBlur,
                ShadowOffsetY = effect.ShadowOffsetY,
                IsHovered = isHovered,
                ShowRunningIndicator = running.Contains(item.Item.Id) ||
                    item.Item.Kind == DockItemKind.Application
            });
        }

        return result;
    }
}

internal sealed class IconEffectInput
{
    public double Scale { get; init; } = 1.0;
    public IconEffectKind IconEffect { get; init; }
    public bool IconImmersion { get; init; }
    public bool IsHovered { get; init; }
    public bool ReflectionEnabled { get; init; } = true;
    public double ReflectionOpacity { get; init; } = 0.35;
    public double ReflectionBlur { get; init; } = 0.5;
}

internal sealed class IconEffectOutput
{
    public double LightIntensity { get; init; }
    public double SelectGlow { get; init; }
    public double ImmersionTilt { get; init; }
    public double ReflectionOpacity { get; init; }
    public double ReflectionOffsetY { get; init; }
    public double ShadowOpacity { get; init; }
    public double ShadowBlur { get; init; }
    public double ShadowOffsetY { get; init; }
    public double VerticalLift { get; init; }
}

internal sealed class IconEffectProcessor
{
    public IconEffectOutput Compute(IconEffectInput input)
    {
        var scaleFactor = Math.Clamp((input.Scale - 1.0) / 0.5, 0, 1);
        var hoverBoost = input.IsHovered ? 0.15 : 0;

        var light = input.IconEffect switch
        {
            IconEffectKind.Light => 0.25 + scaleFactor * 0.45 + hoverBoost,
            IconEffectKind.ScaleLight => 0.15 + scaleFactor * 0.35 + hoverBoost,
            IconEffectKind.Select => 0,
            _ => scaleFactor * 0.12
        };

        var selectGlow = input.IconEffect switch
        {
            IconEffectKind.Select => 0.35 + scaleFactor * 0.4 + hoverBoost,
            IconEffectKind.ScaleLight => 0.12 + scaleFactor * 0.2,
            _ => input.IsHovered ? 0.18 : 0
        };

        var immersionTilt = input.IconImmersion
            ? (input.Scale - 1.0) * 8.0 + (input.IsHovered ? 2.0 : 0)
            : 0;

        var reflectionOpacity = input.ReflectionEnabled
            ? input.ReflectionOpacity * (0.6 + scaleFactor * 0.4)
            : 0;

        var shadowOpacity = 0.12 + scaleFactor * 0.22 + (input.IsHovered ? 0.08 : 0);
        var shadowBlur = 6 + scaleFactor * 14;
        var shadowOffset = 2 + scaleFactor * 6;
        var verticalLift = input.IconImmersion ? -(input.Scale - 1.0) * 4.0 : 0;

        return new IconEffectOutput
        {
            LightIntensity = light,
            SelectGlow = selectGlow,
            ImmersionTilt = immersionTilt,
            ReflectionOpacity = reflectionOpacity,
            ReflectionOffsetY = 4 + scaleFactor * 6,
            ShadowOpacity = shadowOpacity,
            ShadowBlur = shadowBlur,
            ShadowOffsetY = shadowOffset,
            VerticalLift = verticalLift
        };
    }
}
