namespace BndzFinder.Interop;

/// <summary>
/// Replaces the native Windows taskbar with the Bndz-Finder dock while the shell is running.
/// The dock is not an overlay — it registers AppBar space and keeps the system taskbar hidden.
/// </summary>
public interface ITaskbarLifecycleService
{
    void EnableReplacementMode();
    void SyncWithDock(bool dockVisible);
    void Restore();
}

public sealed class TaskbarLifecycleService : ITaskbarLifecycleService
{
    private readonly ITaskbarController _taskbar;
    private readonly Func<bool> _hideWhenDockShown;
    private readonly Func<bool> _allMonitors;
    private readonly Func<bool> _autoHideAtStartup;
    private bool _taskbarHidden;
    private bool _replacementMode;
    private System.Timers.Timer? _watchdog;

    public TaskbarLifecycleService(
        ITaskbarController taskbar,
        Func<bool> hideWhenDockShown,
        Func<bool> allMonitors,
        Func<bool>? autoHideAtStartup = null)
    {
        _taskbar = taskbar;
        _hideWhenDockShown = hideWhenDockShown;
        _allMonitors = allMonitors;
        _autoHideAtStartup = autoHideAtStartup ?? (() => true);
    }

    public void EnableReplacementMode()
    {
        _replacementMode = true;
        if (!OperatingSystem.IsWindows()) return;

        ApplyHide();
        _watchdog ??= new System.Timers.Timer(3000) { AutoReset = true, Enabled = true };
        _watchdog.Elapsed += (_, _) =>
        {
            if (_replacementMode && (_hideWhenDockShown() || _autoHideAtStartup()))
                ApplyHide();
        };
    }

    public void SyncWithDock(bool dockVisible)
    {
        if (_replacementMode)
        {
            if (_hideWhenDockShown() || dockVisible)
                ApplyHide();
            return;
        }

        if (!_hideWhenDockShown())
        {
            if (_taskbarHidden)
                Restore();
            return;
        }

        if (dockVisible)
            ApplyHide();
        else if (_taskbarHidden)
            Restore();
    }

    public void Restore()
    {
        _replacementMode = false;
        _watchdog?.Stop();
        _watchdog?.Dispose();
        _watchdog = null;
        _taskbar.Show(_allMonitors());
        _taskbarHidden = false;
    }

    private void ApplyHide()
    {
        _taskbar.Hide(_allMonitors());
        _taskbarHidden = true;
    }
}
