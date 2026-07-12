using System.Runtime.InteropServices;
using System.Text;
using BndzFinder.Shell.Services;

namespace BndzFinder.Shell.Badges;

public interface IBadgeAdapter
{
    string AppId { get; }
    Task<int?> GetUnreadCountAsync(CancellationToken cancellationToken = default);
}

public class ProcessWindowBadgeAdapter : IBadgeAdapter
{
    private readonly string _processName;

    public ProcessWindowBadgeAdapter(string appId, string processName)
    {
        AppId = appId;
        _processName = processName;
    }

    public string AppId { get; }

    public Task<int?> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
            return Task.FromResult<int?>(null);

        var titles = new List<string>();
        EnumWindows((hwnd, _) =>
        {
            if (!IsEligibleWindow(hwnd)) return true;
            if (!TryGetProcessName(hwnd, out var processName)) return true;
            if (!processName.Equals(_processName, StringComparison.OrdinalIgnoreCase)) return true;
            titles.Add(GetWindowTitle(hwnd));
            return true;
        }, nint.Zero);

        return Task.FromResult(WindowTitleBadgeParser.ParseUnreadCount(titles));
    }

    private static bool TryGetProcessName(nint hwnd, out string processName)
    {
        processName = string.Empty;
        _ = GetWindowThreadProcessId(hwnd, out var pid);
        if (pid == 0) return false;
        try
        {
            using var process = System.Diagnostics.Process.GetProcessById((int)pid);
            processName = process.ProcessName;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsEligibleWindow(nint hwnd)
    {
        if (!IsWindowVisible(hwnd)) return false;
        if (GetWindow(hwnd, 4) != nint.Zero) return false;
        var style = (uint)GetWindowLong(hwnd, -16);
        if ((style & 0x10000000) == 0) return false;
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

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);
}

public sealed class DiscordBadgeAdapter : ProcessWindowBadgeAdapter
{
    public DiscordBadgeAdapter() : base("Discord", "Discord") { }
}

public sealed class WeChatBadgeAdapter : ProcessWindowBadgeAdapter
{
    public WeChatBadgeAdapter() : base("WeChat", "WeChat") { }
}

public sealed class BadgeAdapterRegistry
{
    private readonly IReadOnlyList<IBadgeAdapter> _adapters;

    public BadgeAdapterRegistry(IEnumerable<IBadgeAdapter>? adapters = null)
    {
        _adapters = adapters?.ToList() ?? new List<IBadgeAdapter>
        {
            new DiscordBadgeAdapter(),
            new WeChatBadgeAdapter(),
            new ProcessWindowBadgeAdapter("QQ", "QQ"),
            new ProcessWindowBadgeAdapter("TIM", "TIM"),
            new ProcessWindowBadgeAdapter("DingTalk", "DingTalk"),
            new ProcessWindowBadgeAdapter("AliWangwang", "AliWangwang"),
            new ProcessWindowBadgeAdapter("YY", "YY")
        };
    }

    public IReadOnlyList<IBadgeAdapter> Adapters => _adapters;

    public async Task<IReadOnlyDictionary<string, int?>> PollAllAsync(CancellationToken ct = default)
    {
        var result = new Dictionary<string, int?>(StringComparer.OrdinalIgnoreCase);
        foreach (var adapter in _adapters)
            result[adapter.AppId] = await adapter.GetUnreadCountAsync(ct).ConfigureAwait(false);
        return result;
    }
}
