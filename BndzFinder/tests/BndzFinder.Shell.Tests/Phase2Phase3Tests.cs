using BndzFinder.Core.Models;
using BndzFinder.Interop;
using BndzFinder.Shell.Dock;
using Xunit;

namespace BndzFinder.Shell.Tests;

public class FolderStackServiceTests
{
    [Fact]
    public void BuildLayout_Fan_AssignsAngles()
    {
        var service = new FolderStackService();
        var paths = new[] { "/a", "/b", "/c" };
        var layout = service.BuildLayout(paths, FolderStackView.Fan);
        Assert.Equal(3, layout.Count);
        Assert.NotEqual(layout[0].FanAngle, layout[2].FanAngle);
    }

    [Fact]
    public void BuildLayout_Grid_AssignsGridPositions()
    {
        var service = new FolderStackService();
        var paths = Enumerable.Range(0, 6).Select(i => $"/file{i}").ToList();
        var layout = service.BuildLayout(paths, FolderStackView.Grid);
        Assert.Equal(0, layout[0].GridX);
        Assert.True(layout[4].GridY > 0);
    }

    [Fact]
    public void Automatic_UsesFanForSmallFolders()
    {
        Assert.Equal(FolderStackView.Fan, FolderStackLayoutHelper.ResolveView(FolderStackView.Automatic, 3));
        Assert.Equal(FolderStackView.Grid, FolderStackLayoutHelper.ResolveView(FolderStackView.Automatic, 12));
    }
}

public class HotkeyBindingServiceTests
{
    [Fact]
    public void Register_AddsBinding()
    {
        var hotkeys = new GlobalHotkeyService();
        var service = new HotkeyBindingService(hotkeys);
        service.Register("test", "Win", "L", () => { });
        Assert.Single(service.GetBindings());
        Assert.Equal("test", service.GetBindings()[0].Id);
    }

    [Fact]
    public void Unregister_RemovesBinding()
    {
        var hotkeys = new GlobalHotkeyService();
        var service = new HotkeyBindingService(hotkeys);
        service.Register("test", "Win", "L", () => { });
        service.Unregister("test");
        Assert.Empty(service.GetBindings());
    }
}

public class AppBarServiceTests
{
    [Fact]
    public void QueryPosition_NonWindows_ReturnsDefault()
    {
        var service = new AppBarService();
        var rect = service.QueryPosition(nint.Zero, AppBarEdge.Bottom, 64);
        Assert.Equal(0, rect.Left);
    }
}
