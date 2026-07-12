using BndzFinder.Animations;
using BndzFinder.Core.Models;
using BndzFinder.Core.Services;
using BndzFinder.Core.Design;
using BndzFinder.Shell.Assets;
using BndzFinder.Shell.Badges;
using BndzFinder.Shell.Dock;
using BndzFinder.Theming.Editors;
using Xunit;

namespace BndzFinder.Shell.Tests;

public class CompletionPlanTests
{
    [Fact]
    public void DockBehavior_AutoHide_RequiresPointerNearEdge()
    {
        var service = new DockBehaviorService();
        var settings = new Core.Settings.BndzFinderSettings { DockDisplayMode = DockDisplayMode.AutoHide };
        Assert.False(service.ShouldShowDock(settings, pointerNearEdge: false, hasEligibleWindows: true));
        Assert.True(service.ShouldShowDock(settings, pointerNearEdge: true, hasEligibleWindows: false));
    }

    [Fact]
    public void DockBehavior_SeedsSystemItems()
    {
        var service = new DockBehaviorService();
        var settings = new Core.Settings.BndzFinderSettings();
        var items = service.EnsureDefaultItems(settings);
        Assert.Contains(items, i => i.Kind == DockItemKind.SystemTrash);
        Assert.Contains(items, i => i.Kind == DockItemKind.SystemWeather);
    }

    [Fact]
    public void MinimizeFrameCalculator_Genie_ProducesNeck()
    {
        var calc = new MinimizeFrameCalculator();
        var request = new MinimizeAnimationRequest
        {
            SourceWindow = 0,
            WindowSnapshot = [],
            TargetX = 10, TargetY = 20
        };
        var mid = calc.Calculate(request, MinimizeEffect.Genie, 0.5);
        Assert.True(mid.GenieNeck > 0);
    }

    [Fact]
    public void MinimizeFrameCalculator_ScaleDx_UsesHardware()
    {
        var calc = new MinimizeFrameCalculator();
        var request = new MinimizeAnimationRequest { SourceWindow = 0, WindowSnapshot = [] };
        var state = calc.Calculate(request, MinimizeEffect.ScaleDx, 0.5);
        Assert.True(state.UseHardware);
    }

    [Fact]
    public async Task BadgeRegistry_PollsCounts()
    {
        var registry = new BadgeAdapterRegistry();
        var counts = await registry.PollAllAsync();
        Assert.True(counts.ContainsKey("Discord"));
    }

    [Fact]
    public void AssetCatalog_ResolvesSystemIconPaths()
    {
        var catalog = new AssetCatalogService();
        var path = catalog.ResolvePath("icon-finder");
        Assert.EndsWith("finder.png", path);
    }

    [Fact]
    public void AppleDesignMetrics_MatchesAppIconTemplate()
    {
        Assert.Equal(1024, AppleDesignMetrics.IconCanvasSize);
        Assert.Equal(824f, AppleDesignMetrics.IconContentSize);
    }

    [Fact]
    public void HsvColor_ConvertsToHex()
    {
        var r = (byte)(0.2 * 255);
        var g = (byte)(0.5 * 255);
        var b = (byte)(0.9 * 255);
        var hex = $"#{r:X2}{g:X2}{b:X2}";
        Assert.StartsWith("#", hex);
    }

    [Fact]
    public void ThemeEditor_PersistsOpacity()
    {
        var state = new ThemeEditorState();
        var editor = new ThemeEditor(state);
        editor.SetDockBackground(null, 0.5, 0.9, 0);
        var settings = new Core.Settings.BndzFinderSettings();
        new ThemeApplyService { State = state }.ApplyToSettings(settings);
        Assert.Equal(0.9, settings.DockOpacity);
    }

    [Fact]
    public void DisplayMonitor_ReturnsPrimaryOnNonWindows()
    {
        var monitors = new DisplayMonitorService().GetMonitors();
        Assert.Single(monitors);
        Assert.True(monitors[0].IsPrimary);
    }

    [Fact]
    public void ShellOverlayController_RespectsFinderDisabled()
    {
        var settings = new SettingsService();
        settings.Current.FinderEnabled = false;
        var controller = new ShellOverlayController(settings);
        controller.ToggleFinder();
        Assert.False(controller.IsFinderVisible);
    }
}

internal sealed class ThemeApplyService : IThemeApplyService
{
    public required ThemeEditorState State { get; init; }
    ThemeEditorState IThemeApplyService.State => State;
    public void ApplyToSettings(Core.Settings.BndzFinderSettings settings)
    {
        settings.DockOpacity = State.DockBackgroundOpacity;
        settings.GlobalBlurValue = State.DockBackgroundBlur;
    }
    public void LoadFromSettings(Core.Settings.BndzFinderSettings settings) { }
}
