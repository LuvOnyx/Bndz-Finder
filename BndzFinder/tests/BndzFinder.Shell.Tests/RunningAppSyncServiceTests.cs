using BndzFinder.Core.Models;
using BndzFinder.Shell.Services;
using Xunit;

namespace BndzFinder.Shell.Tests;

public class RunningAppSyncServiceTests
{
    [Fact]
    public void MergeDockItems_AddsEphemeralRunningApps()
    {
        var service = new RunningAppSyncService();
        var pinned = new List<DockItem>
        {
            new() { Id = "finder", Kind = DockItemKind.SystemFinder, TargetPath = "SystemFinder", DisplayName = "Finder", SortOrder = 0 }
        };

        var running =
            new List<RunningAppInfo>
            {
                new() { Id = "running:c:\\apps\\code.exe", ExePath = @"C:\Apps\Code.exe", DisplayName = "Code", MainWindow = 0 }
            };

        var merged = service.MergeDockItems(pinned, running, out var runningIds);
        Assert.Equal(2, merged.Count);
        Assert.Contains(merged, i => i.DisplayName == "Code" && !i.IsPinned);
        Assert.Contains(runningIds, id => id.Contains("code.exe", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MergeDockItems_MarksPinnedRunningApp()
    {
        var service = new RunningAppSyncService();
        var pinned = new List<DockItem>
        {
            new()
            {
                Id = "code",
                Kind = DockItemKind.Application,
                TargetPath = @"C:\Apps\Code.exe",
                DisplayName = "Code",
                IsPinned = true,
                SortOrder = 0
            }
        };

        var running = new List<RunningAppInfo>
        {
            new() { Id = "running:c:\\apps\\code.exe", ExePath = @"C:\Apps\Code.exe", DisplayName = "Code", MainWindow = 0 }
        };

        var merged = service.MergeDockItems(pinned, running, out var runningIds);
        Assert.Single(merged);
        Assert.Contains("code", runningIds);
    }
}
