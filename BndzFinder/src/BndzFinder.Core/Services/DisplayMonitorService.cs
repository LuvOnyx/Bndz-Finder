namespace BndzFinder.Core.Services;

public sealed class DisplayMonitorInfo
{
    public required string Name { get; init; }
    public int Left { get; init; }
    public int Top { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public bool IsPrimary { get; init; }
}

public interface IDisplayMonitorService
{
    IReadOnlyList<DisplayMonitorInfo> GetMonitors();
    DisplayMonitorInfo? ResolveMonitor(string? monitorName);
}

public sealed class DisplayMonitorService : IDisplayMonitorService
{
    public IReadOnlyList<DisplayMonitorInfo> GetMonitors()
    {
        if (!OperatingSystem.IsWindows()) return [DefaultMonitor()];
        return WindowsMonitors.Enumerate();
    }

    public DisplayMonitorInfo? ResolveMonitor(string? monitorName)
    {
        var monitors = GetMonitors();
        if (string.IsNullOrWhiteSpace(monitorName))
            return monitors.FirstOrDefault(m => m.IsPrimary) ?? monitors.FirstOrDefault();
        return monitors.FirstOrDefault(m => m.Name.Equals(monitorName, StringComparison.OrdinalIgnoreCase))
            ?? monitors.FirstOrDefault(m => m.IsPrimary);
    }

    private static DisplayMonitorInfo DefaultMonitor() => new()
    {
        Name = "Primary",
        Left = 0, Top = 0, Width = 1920, Height = 1080,
        IsPrimary = true
    };
}

internal static class WindowsMonitors
{
    private static readonly List<DisplayMonitorInfo> Buffer = [];

    public static List<DisplayMonitorInfo> Enumerate()
    {
        Buffer.Clear();
        EnumDisplayMonitors(nint.Zero, nint.Zero, Callback, nint.Zero);
        return Buffer.Count > 0 ? [..Buffer] : [new DisplayMonitorInfo
        {
            Name = "Primary", Left = 0, Top = 0,
            Width = GetSystemMetrics(0), Height = GetSystemMetrics(1), IsPrimary = true
        }];
    }

    private static nint Callback(nint hMonitor, nint hdc, ref NativeRect rc, nint data)
    {
        var info = new MonitorInfoEx { Size = System.Runtime.InteropServices.Marshal.SizeOf<MonitorInfoEx>() };
        if (GetMonitorInfo(hMonitor, ref info))
        {
            Buffer.Add(new DisplayMonitorInfo
            {
                Name = info.DeviceName.Trim(),
                Left = rc.Left, Top = rc.Top,
                Width = rc.Right - rc.Left, Height = rc.Bottom - rc.Top,
                IsPrimary = (info.Flags & 1) != 0
            });
        }
        return 1;
    }

    private delegate nint MonitorEnumProc(nint hMonitor, nint hdc, ref NativeRect lprcMonitor, nint dwData);

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left, Top, Right, Bottom;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private struct MonitorInfoEx
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect WorkArea;
        public uint Flags;
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(nint hdc, nint lprcClip, MonitorEnumProc lpfnEnum, nint dwData);

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern bool GetMonitorInfo(nint hMonitor, ref MonitorInfoEx lpmi);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);
}
