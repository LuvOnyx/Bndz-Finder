using BndzFinder.Core.Services;
using BndzFinder.Interop;
using BndzFinder.Shell.Services;
using Xunit;

namespace BndzFinder.Shell.Tests;

public class SprintCFeatureTests
{
    [Fact]
    public void WindowTitleBadgeParser_ParsesParentheticalCounts()
    {
        var count = WindowTitleBadgeParser.ParseUnreadCount(["Discord (12)", "General"]);
        Assert.Equal(12, count);
    }

    [Fact]
    public void WindowTitleBadgeParser_ReturnsNullWhenNoCounts()
    {
        var count = WindowTitleBadgeParser.ParseUnreadCount(["Discord", "General"]);
        Assert.Null(count);
    }

    [Fact]
    public void BgraPngEncoder_ReturnsEmptyForInvalidInput()
    {
        Assert.Empty(BgraPngEncoder.EncodePng([], 0, 0));
        Assert.Equal(string.Empty, BgraPngEncoder.EncodePngBase64([], 0, 0));
    }

    [Fact]
    public void ProgressPayload_RoundTrips()
    {
        var json = ProgressPayload.Serialize([
            new ProgressEntrySnapshot { ExePath = @"C:\Apps\Example.exe", Value = 0.42 }
        ]);
        var parsed = ProgressPayload.Deserialize(json);
        Assert.Single(parsed);
        Assert.Equal(0.42, parsed[0].Value);
    }

    [Fact]
    public void TaskbarProgress_ParsePercent_FromTitle()
    {
        Assert.Equal(0.42, TaskbarProgressService.ParsePercent("Copying files 42%"));
        Assert.Null(TaskbarProgressService.ParsePercent("No progress here"));
    }
}
