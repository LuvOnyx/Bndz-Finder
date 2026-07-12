namespace BndzFinder.Interop;

/// <summary>
/// Hides/shows the native Windows taskbar so the Bndz-Finder dock can replace it.
/// Synced with dock visibility — not a one-shot autohide at startup.
/// </summary>
public interface ITaskbarLifecycleService
{
    void SyncWithDock(bool dockVisible);
    void Restore();
}

public sealed class TaskbarLifecycleService : ITaskbarLifecycleService
{
    private readonly ITaskbarController _taskbar;
    private readonly Func<bool> _hideWhenDockShown;
    private readonly Func<bool> _allMonitors;
    private bool _taskbarHidden;

    public TaskbarLifecycleService(
        ITaskbarController taskbar,
        Func<bool> hideWhenDockShown,
        Func<bool> allMonitors)
    {
        _taskbar = taskbar;
        _hideWhenDockShown = hideWhenDockShown;
        _allMonitors = allMonitors;
    }

    public void SyncWithDock(bool dockVisible)
    {
        if (!_hideWhenDockShown())
        {
            if (_taskbarHidden)
                Restore();
            return;
        }

        if (dockVisible)
        {
            _taskbar.Hide(_allMonitors());
            _taskbarHidden = true;
        }
        else if (_taskbarHidden)
        {
            Restore();
        }
    }

    public void Restore()
    {
        _taskbar.Show(_allMonitors());
        _taskbarHidden = false;
    }
}
