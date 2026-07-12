using System.Runtime.InteropServices;
using BndzFinder.Core.Models;
using BndzFinder.Core.Services;

namespace BndzFinder.Interop;

public interface IMinimizeHookService
{
    void Start(Action<nint> onMinimize);
    void Stop();
}

public sealed class MinimizeHookService : IMinimizeHookService, IDisposable
{
    private HookProc? _proc;
    private nint _hook;
    private Action<nint>? _callback;

    private delegate nint HookProc(int code, nint wParam, nint lParam);

    public void Start(Action<nint> onMinimize)
    {
        if (!OperatingSystem.IsWindows()) return;
        _callback = onMinimize;
        _proc = HookCallback;
        _hook = SetWindowsHookEx(5, _proc, GetModuleHandle(null), 0);
    }

    public void Stop()
    {
        if (_hook != nint.Zero)
        {
            UnhookWindowsHookEx(_hook);
            _hook = nint.Zero;
        }
    }

    private nint HookCallback(int code, nint wParam, nint lParam)
    {
        if (code == 1 && lParam != nint.Zero) // HCBT_MINMAX
        {
            var showCmd = Marshal.ReadInt32(lParam, IntPtr.Size == 8 ? 16 : 8);
            if (showCmd == 6) // SW_MINIMIZE
            {
                _callback?.Invoke(wParam);
            }
        }
        return CallNextHookEx(_hook, code, wParam, lParam);
    }

    public void Dispose() => Stop();

    [DllImport("user32.dll")]
    private static extern nint SetWindowsHookEx(int idHook, HookProc lpfn, nint hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(nint hhk);

    [DllImport("user32.dll")]
    private static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern nint GetModuleHandle(string? lpModuleName);
}

public interface IHotkeyBindingService
{
    void Register(string bindingId, string? modifiers, string? key, Action handler);
    void Unregister(string bindingId);
    IReadOnlyList<HotkeyBinding> GetBindings();
}

public sealed class HotkeyBindingService : IHotkeyBindingService, IHotkeyBindingRegistrar
{
    private readonly Dictionary<string, (HotkeyBinding Binding, Action Handler)> _bindings = new();
    private readonly Dictionary<string, int> _bindingToHotkeyId = new();
    private readonly GlobalHotkeyService _hotkeys;
    private int _nextId = 1;

    public HotkeyBindingService(GlobalHotkeyService hotkeys) => _hotkeys = hotkeys;

    public void Register(string bindingId, string? modifiers, string? key, Action handler) =>
        Register(new HotkeyBinding { Id = bindingId, Modifiers = modifiers, Key = key }, handler);

    public void Register(HotkeyBinding binding, Action handler)
    {
        if (_bindingToHotkeyId.TryGetValue(binding.Id, out var existingId))
            _hotkeys.Unregister(existingId);

        var id = _nextId++;
        _bindings[binding.Id] = (binding, handler);
        _bindingToHotkeyId[binding.Id] = id;
        var modFlags = ParseModifiers(binding.Modifiers);
        var vk = ParseVirtualKey(binding.Key);
        if (vk != 0)
            _hotkeys.TryRegister(id, modFlags, vk, handler);
    }

    public void Unregister(string bindingId)
    {
        if (_bindings.Remove(bindingId, out _) && _bindingToHotkeyId.Remove(bindingId, out var id))
            _hotkeys.Unregister(id);
    }

    public IReadOnlyList<HotkeyBinding> GetBindings() =>
        _bindings.Values.Select(v => v.Binding).ToList();

    private static uint ParseModifiers(string? modifiers)
    {
        if (string.IsNullOrEmpty(modifiers)) return 0;
        uint flags = 0;
        if (modifiers.Contains("Ctrl", StringComparison.OrdinalIgnoreCase)) flags |= 0x0002;
        if (modifiers.Contains("Alt", StringComparison.OrdinalIgnoreCase)) flags |= 0x0001;
        if (modifiers.Contains("Shift", StringComparison.OrdinalIgnoreCase)) flags |= 0x0004;
        if (modifiers.Contains("Win", StringComparison.OrdinalIgnoreCase)) flags |= 0x0008;
        return flags;
    }

    private static uint ParseVirtualKey(string? key) => key?.ToUpperInvariant() switch
    {
        "L" => 0x4C,
        "D" => 0x44,
        "M" => 0x4D,
        "F11" => 0x7A,
        _ => 0
    };
}

public interface IHotCornerMonitor
{
    void Start(HotCornerBinding corners, Action<HotCornerAction, bool> onTriggered);
    void Stop();
}

public sealed class HotCornerMonitor : IHotCornerMonitor, IDisposable
{
    private CancellationTokenSource? _cts;

    public void Start(HotCornerBinding corners, Action<HotCornerAction, bool> onTriggered)
    {
        _cts = new CancellationTokenSource();
        _ = MonitorAsync(corners, onTriggered, _cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts = null;
    }

    private static async Task MonitorAsync(HotCornerBinding corners, Action<HotCornerAction, bool> onTriggered, CancellationToken ct)
    {
        var cornerSize = 8;
        var active = new HashSet<HotCornerAction>();

        while (!ct.IsCancellationRequested)
        {
            if (OperatingSystem.IsWindows())
            {
                if (GetCursorPos(out var point))
                {
                    var screenWidth = GetSystemMetrics(0);
                    var screenHeight = GetSystemMetrics(1);
                    var hits = new List<(HotCornerAction Action, bool Hit)>();
                    if (corners.BottomLeft is not HotCornerAction.None and var leftAction)
                        hits.Add((leftAction, point.X <= cornerSize && point.Y >= screenHeight - cornerSize));
                    if (corners.BottomRight is not HotCornerAction.None and var rightAction)
                        hits.Add((rightAction, point.X >= screenWidth - cornerSize && point.Y >= screenHeight - cornerSize));

                    foreach (var (action, hit) in hits)
                    {
                        if (hit && active.Add(action))
                            onTriggered(action, true);
                        else if (!hit && active.Remove(action))
                            onTriggered(action, false);
                    }
                }
            }

            await Task.Delay(100, ct).ConfigureAwait(false);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    public void Dispose() => Stop();
}
