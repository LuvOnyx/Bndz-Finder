using System.Runtime.InteropServices;
using System.Text;

namespace BndzFinder.Interop;

public sealed class WindowThumbnailItem
{
    public required nint Hwnd { get; init; }
    public required string Title { get; init; }
    public byte[]? Thumbnail { get; init; }
}

public static class WindowEnumerationService
{
    public static IReadOnlyList<WindowThumbnailItem> GetOpenWindows(IReadOnlyList<string> blacklist)
    {
        if (!OperatingSystem.IsWindows()) return [];

        var excluded = blacklist.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var results = new List<WindowThumbnailItem>();

        EnumWindows((hwnd, _) =>
        {
            if (!IsEligibleWindow(hwnd)) return true;
            var title = GetWindowTitle(hwnd);
            if (string.IsNullOrWhiteSpace(title) || excluded.Contains(title)) return true;
            results.Add(new WindowThumbnailItem { Hwnd = hwnd, Title = title });
            return true;
        }, nint.Zero);

        return results;
    }

    private static bool IsEligibleWindow(nint hwnd)
    {
        if (!IsWindowVisible(hwnd)) return false;
        if (GetWindow(hwnd, 4) != nint.Zero) return false; // GW_OWNER
        var style = (uint)GetWindowLong(hwnd, -16); // GWL_STYLE
        if ((style & 0x10000000) == 0) return false; // WS_VISIBLE
        if ((style & 0x00C00000) == 0x00C00000) return false; // WS_POPUP child
        return true;
    }

    private static string GetWindowTitle(nint hwnd)
    {
        var length = GetWindowTextLength(hwnd);
        if (length == 0) return string.Empty;
        var sb = new StringBuilder(length + 1);
        _ = GetWindowText(hwnd, sb, sb.Capacity);
        return sb.ToString();
    }

    private delegate bool EnumWindowsProc(nint hwnd, nint lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(nint hWnd);

    [DllImport("user32.dll")]
    private static extern nint GetWindow(nint hWnd, uint uCmd);

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern int GetWindowLong(nint hWnd, int nIndex);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(nint hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(nint hWnd);
}
