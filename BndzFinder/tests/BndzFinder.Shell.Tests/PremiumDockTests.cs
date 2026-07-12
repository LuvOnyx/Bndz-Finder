using BndzFinder.Core.Models;
using BndzFinder.Shell.Dock;
using BndzFinder.Theming.Customization;
using BndzFinder.Theming.Glass;
using Xunit;

namespace BndzFinder.Shell.Tests;

public class PremiumDockLayoutEngineTests
{
    private static IReadOnlyList<Core.Models.DockItem> SampleItems() =>
    [
        new() { Id = "a", Kind = Core.Models.DockItemKind.Application, TargetPath = "a.exe", SortOrder = 0 },
        new() { Id = "b", Kind = Core.Models.DockItemKind.Application, TargetPath = "b.exe", SortOrder = 1 },
        new() { Id = "c", Kind = Core.Models.DockItemKind.Application, TargetPath = "c.exe", SortOrder = 2 },
        new() { Id = "sep", Kind = Core.Models.DockItemKind.Separator, TargetPath = "", SortOrder = 3 },
        new() { Id = "d", Kind = Core.Models.DockItemKind.Application, TargetPath = "d.exe", SortOrder = 4 }
    ];

    [Fact]
    public void ComputeLayout_HoveredIcon_HasLightAndReflection()
    {
        var engine = new PremiumDockLayoutEngine();
        var layout = engine.ComputeLayout(new PremiumDockLayoutRequest
        {
            Items = SampleItems(),
            CursorPosition = 56,
            BaseIconSize = 48,
            MaxIconSize = 72,
            IconEffect = IconEffectKind.Light,
            IconImmersion = true,
            ReflectionEnabled = true,
            ReflectionOpacity = 0.35,
            HoveredIndex = 1
        });

        var hovered = layout[1];
        Assert.True(hovered.LightIntensity > 0);
        Assert.True(hovered.ReflectionOpacity > 0);
        Assert.True(hovered.ImmersionTilt != 0);
        Assert.True(hovered.IsHovered);
    }

    [Fact]
    public void ComputeLayout_SelectEffect_AddsGlow()
    {
        var engine = new PremiumDockLayoutEngine();
        var layout = engine.ComputeLayout(new PremiumDockLayoutRequest
        {
            Items = SampleItems(),
            CursorPosition = 56,
            IconEffect = IconEffectKind.Select,
            HoveredIndex = 1
        });

        Assert.True(layout[1].SelectGlow > 0);
    }

    [Fact]
    public void ComputeLayout_MagnificationDisabled_UniformSize()
    {
        var engine = new PremiumDockLayoutEngine();
        var layout = engine.ComputeLayout(new PremiumDockLayoutRequest
        {
            Items = SampleItems(),
            CursorPosition = 56,
            MagnificationEnabled = false,
            BaseIconSize = 48,
            MaxIconSize = 72
        });

        Assert.All(layout.Where(i => !i.IsSeparator), item => Assert.Equal(48, item.Size));
    }
}

public class GlassBackdropServiceTests
{
    [Theory]
    [InlineData(GlassEffectKind.Translucent, 0)]
    [InlineData(GlassEffectKind.Acrylic, 0.6)]
    [InlineData(GlassEffectKind.Mica, 0.6)]
    [InlineData(GlassEffectKind.LiquidGlass, 0.6)]
    public void Resolve_ReturnsValidConfiguration(GlassEffectKind effect, double blur)
    {
        var service = new GlassBackdropService();
        var config = service.Resolve(effect, new GlassCustomizationInput
        {
            GlobalBlur = blur,
            IsDark = true
        });

        Assert.Equal(effect, config.Effect);
        Assert.True(config.CornerRadius >= 20);
        Assert.InRange(config.Opacity, 0.5, 1.0);
    }

    [Fact]
    public void ThemeResolver_BuildsDockAppearance_WithAccent()
    {
        var resolver = new ThemeResolver();
        var profile = resolver.BuildDockAppearance(new DockAppearanceInput
        {
            ThemeMode = ThemeModeKind.Dark,
            AccentColor = "#FF5500",
            GlassEffect = GlassEffectKind.Acrylic,
            IconEffect = IconEffectKind.ScaleLight,
            IconImmersion = true,
            DpiScale = 1.25
        });

        Assert.True(profile.IsDark);
        Assert.Equal(0xFF, profile.Accent.R);
        Assert.Equal(IconEffectKind.ScaleLight, profile.IconEffect);
        Assert.Equal(60, profile.ScaledBaseIconSize);
    }
}
