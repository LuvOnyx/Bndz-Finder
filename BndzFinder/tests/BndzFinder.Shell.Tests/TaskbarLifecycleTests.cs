using BndzFinder.Interop;
using Xunit;

namespace BndzFinder.Shell.Tests;

public class TaskbarLifecycleTests
{
    [Fact]
    public void SyncWithDock_HidesWhenVisibleAndEnabled()
    {
        var controller = new RecordingTaskbarController();
        var lifecycle = new TaskbarLifecycleService(controller, () => true, () => true);

        lifecycle.SyncWithDock(dockVisible: true);

        Assert.True(controller.HideCalled);
        Assert.True(controller.LastAllMonitors);
    }

    [Fact]
    public void SyncWithDock_RestoresWhenDockHidden()
    {
        var controller = new RecordingTaskbarController();
        var lifecycle = new TaskbarLifecycleService(controller, () => true, () => false);

        lifecycle.SyncWithDock(true);
        lifecycle.SyncWithDock(false);

        Assert.True(controller.ShowCalled);
    }

    [Fact]
    public void ReplacementMode_KeepsTaskbarHiddenWhenDockHides()
    {
        var controller = new RecordingTaskbarController();
        var lifecycle = new TaskbarLifecycleService(controller, () => true, () => false);
        lifecycle.EnableReplacementMode();

        lifecycle.SyncWithDock(true);
        var hideCount = controller.HideCount;
        lifecycle.SyncWithDock(false);

        Assert.Equal(0, controller.ShowCount);
        Assert.True(controller.HideCount >= hideCount);
    }

    [Fact]
    public void SyncWithDock_SkipsWhenSettingDisabled()
    {
        var controller = new RecordingTaskbarController();
        var lifecycle = new TaskbarLifecycleService(controller, () => false, () => false);

        lifecycle.SyncWithDock(true);

        Assert.False(controller.HideCalled);
    }

    private sealed class RecordingTaskbarController : ITaskbarController
    {
        public int HideCount { get; private set; }
        public int ShowCount { get; private set; }
        public bool HideCalled => HideCount > 0;
        public bool ShowCalled => ShowCount > 0;
        public bool LastAllMonitors { get; private set; }

        public void SetAutoHide(bool enabled) { }

        public void Hide(bool allMonitors = false)
        {
            HideCount++;
            LastAllMonitors = allMonitors;
        }

        public void Show(bool allMonitors = false)
        {
            ShowCount++;
            LastAllMonitors = allMonitors;
        }
    }
}
