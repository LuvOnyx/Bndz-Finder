using BndzFinder.Core.Services;
using BndzFinder.Core.Settings;
using Xunit;

namespace BndzFinder.Core.Tests;

public class SettingsServiceTests
{
    [Fact]
    public async Task SaveAndLoad_RoundTripsSettings()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bndz-test-{Guid.NewGuid():N}.json");
        var service = new SettingsService(path);
        await service.LoadAsync();
        service.Current.IconSize = 64;
        service.Current.DockPosition = Models.DockPosition.Left;
        await service.SaveAsync();

        var reloaded = new SettingsService(path);
        await reloaded.LoadAsync();
        Assert.Equal(64, reloaded.Current.IconSize);
        Assert.Equal(Models.DockPosition.Left, reloaded.Current.DockPosition);
    }

    [Fact]
    public async Task ResetComponent_Dock_ClearsDockItems()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bndz-test-{Guid.NewGuid():N}.json");
        var service = new SettingsService(path);
        await service.LoadAsync();
        service.Current.DockItems.Add(new Models.DockItem
        {
            Id = "1",
            Kind = Models.DockItemKind.Application,
            TargetPath = "C:\\test.exe"
        });
        await service.SaveAsync();
        await service.ResetComponentAsync("dock");
        Assert.Empty(service.Current.DockItems);
    }
}
