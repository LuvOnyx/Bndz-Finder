using BndzFinder.Core.Models;
using BndzFinder.Shell.Services;
using Xunit;

namespace BndzFinder.Shell.Tests;

public class Phase5FeatureTests
{
    [Fact]
    public void WeatherService_ReturnsCondition()
    {
        var weather = new WeatherService();
        Assert.False(string.IsNullOrWhiteSpace(weather.GetCurrentCondition()));
        Assert.Equal(3, weather.GetForecast().Count);
    }

    [Fact]
    public void ProgressBarMirror_TracksProgress()
    {
        var progress = new ProgressBarMirrorService();
        progress.SetProgress(@"C:\Apps\Example.exe", 0.42);
        Assert.Equal(0.42, progress.GetProgress(@"C:\Apps\Example.exe"));
    }

    [Fact]
    public void HotkeySync_SkipsEmptyBindings()
    {
        var sync = new Core.Services.HotkeySyncService();
        var registrar = new FakeRegistrar();
        var settings = new Core.Settings.BndzFinderSettings
        {
            DockHotkey = new HotkeyBinding { Id = "dock", Key = "D", Modifiers = "Ctrl" },
            FinderHotkey = new HotkeyBinding { Id = "finder" },
            LaunchpadHotkey = new HotkeyBinding { Id = "launchpad", Key = "L", Modifiers = "Win" },
            StageManagerHotkey = new HotkeyBinding { Id = "stage" }
        };

        sync.ApplyFromSettings(settings, registrar);
        Assert.Equal(2, registrar.Registered.Count);
        Assert.Contains(registrar.Registered, b => b.Id == "launchpad");
    }

    private sealed class FakeRegistrar : Core.Services.IHotkeyBindingRegistrar
    {
        public List<HotkeyBinding> Registered { get; } = [];
        public void Register(HotkeyBinding binding, Action handler) => Registered.Add(binding);
        public void Unregister(string bindingId) => Registered.RemoveAll(b => b.Id == bindingId);
    }
}
