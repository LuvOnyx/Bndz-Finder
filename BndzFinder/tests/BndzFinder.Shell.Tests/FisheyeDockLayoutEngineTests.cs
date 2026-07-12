using BndzFinder.Animations;
using BndzFinder.Core.Models;
using BndzFinder.Shell.Dock;
using Xunit;

namespace BndzFinder.Shell.Tests;

public class FisheyeDockLayoutEngineTests
{
    [Fact]
    public void ComputeLayout_CursorOverIcon_IncreasesSize()
    {
        var engine = new FisheyeDockLayoutEngine();
        var items = new[]
        {
            new Core.Models.DockItem { Id = "a", Kind = Core.Models.DockItemKind.Application, TargetPath = "a.exe", SortOrder = 0 },
            new Core.Models.DockItem { Id = "b", Kind = Core.Models.DockItemKind.Application, TargetPath = "b.exe", SortOrder = 1 },
            new Core.Models.DockItem { Id = "c", Kind = Core.Models.DockItemKind.Application, TargetPath = "c.exe", SortOrder = 2 }
        };

        var layout = engine.ComputeLayout(items, cursorPosition: 56, baseIconSize: 48, maxIconSize: 72, iconSpacing: 8);
        Assert.Equal(3, layout.Count);
        Assert.True(layout[1].Size > layout[0].Size || layout[1].Size > layout[2].Size);
    }
}

public class MinimizeAnimatorFactoryTests
{
    [Fact]
    public void Get_ReturnsAllEffects()
    {
        var factory = new MinimizeAnimatorFactory();
        Assert.Equal(MinimizeEffect.Genie, factory.Get(MinimizeEffect.Genie).Effect);
        Assert.Equal(MinimizeEffect.ScaleDx, factory.Get(MinimizeEffect.ScaleDx).Effect);
        Assert.Equal(MinimizeEffect.Suck, factory.Get(MinimizeEffect.Suck).Effect);
    }
}
