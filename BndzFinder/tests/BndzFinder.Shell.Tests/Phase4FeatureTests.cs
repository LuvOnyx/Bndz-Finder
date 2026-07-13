using BndzFinder.Core.Services;
using Xunit;

namespace BndzFinder.Shell.Tests;

public class Phase4FeatureTests
{
    [Fact]
    public void MinimizeStartedPayload_IncludesTargetCoordinates()
    {
        var json = MinimizeStartedPayload.Serialize(100, 10, 20, 800, 600, "Genie", null, 960, 1000);
        var info = MinimizeStartedPayload.Deserialize(json);
        Assert.NotNull(info);
        Assert.Equal(960, info!.TargetX);
        Assert.Equal(1000, info.TargetY);
        Assert.Equal("Genie", info.Effect);
    }

    [Fact]
    public void ProgressPayload_DeserializesMultipleEntries()
    {
        var json = ProgressPayload.Serialize([
            new ProgressEntrySnapshot { ExePath = @"C:\A.exe", Value = 0.2 },
            new ProgressEntrySnapshot { ExePath = @"C:\B.exe", Value = 0.8 }
        ]);
        var entries = ProgressPayload.Deserialize(json);
        Assert.Equal(2, entries.Count);
        Assert.Equal(0.8, entries[1].Value);
    }
}
