using System.Runtime.InteropServices;
using System.Text;

namespace BndzFinder.Interop;

public sealed class ForegroundAppInfo
{
    public required string Title { get; init; }
    public required string ProcessName { get; init; }
    public nint Hwnd { get; init; }
}

public interface IForegroundAppService
{
    ForegroundAppInfo? GetForegroundApp();
}

/// <summary>
/// Tracks the foreground window for the macOS-style menu bar app title.
/// </summary>
public sealed class ForegroundAppService : IForegroundAppService
{
    public ForegroundAppInfo? GetForegroundApp()
    {
        if (!OperatingSystem.IsWindows()) return null;

        var hwnd = GetForegroundWindow();
        if (hwnd == nint.Zero) return null;

        var title = GetWindowTitle(hwnd);
        _ = GetWindowThreadProcessId(hwnd, out var pid);
        var processName = "Desktop";
        try
        {
            using var process = System.Diagnostics.Process.GetProcessById((int)pid);
            processName = string.IsNullOrWhiteSpace(process.MainModule?.FileVersionInfo.FileDescription)
                ? process.ProcessName
                : process.MainModule!.FileVersionInfo.FileDescription!;
            if (string.IsNullOrWhiteSpace(processName))
                processName = process.ProcessName;
        }
        catch
        {
            // Access-denied processes still show a title if available.
        }

        if (string.Equals(processName, "BndzFinder.App", StringComparison.OrdinalIgnoreCase)
            || string.Equals(processName, "BndzFinder.ShellHost", StringComparison.OrdinalIgnoreCase)
            || processName.Contains("Bndz", StringComparison.OrdinalIgnoreCase))
        {
            processName = "Finder";
        }

        return new ForegroundAppInfo
        {
            Hwnd = hwnd,
            Title = title,
            ProcessName = processName
        };
    }

    private static string GetWindowTitle(nint hwnd)
    {
        var length = GetWindowTextLength(hwnd);
        if (length <= 0) return string.Empty;
        var sb = new StringBuilder(length + 1);
        _ = GetWindowText(hwnd, sb, sb.Capacity);
        return sb.ToString();
    }

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(nint hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(nint hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);
}
