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
    public async Task WeatherService_RefreshKeepsDefaultOnNetworkFailure()
    {
        var weather = new WeatherService();
        await weather.RefreshAsync(999, 999);
        Assert.False(string.IsNullOrWhiteSpace(weather.GetCurrentCondition()));
    }
}
