using System.Text.RegularExpressions;

namespace BndzFinder.Interop;

public sealed class TaskbarProgressEntry
{
    public required string ExePath { get; init; }
    public double Value { get; init; }
    public nint WindowHandle { get; init; }
}

public interface ITaskbarProgressService
{
    IReadOnlyList<TaskbarProgressEntry> GetActiveProgress();
}

/// <summary>
/// Mirrors in-progress operations by scanning window titles and tray tooltips for percentage patterns.
/// </summary>
public sealed class TaskbarProgressService : ITaskbarProgressService
{
    private static readonly Regex PercentPattern = new(@"(\d{1,3})\s*%", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public IReadOnlyList<TaskbarProgressEntry> GetActiveProgress()
    {
        if (!OperatingSystem.IsWindows()) return [];

        var results = new Dictionary<string, TaskbarProgressEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (var window in WindowEnumerationService.GetOpenWindows([]))
        {
            if (!TryGetProcessPath(window.Hwnd, out var exePath)) continue;
            var value = ParsePercent(window.Title);
            if (value is null) continue;

            if (!results.TryGetValue(exePath, out var existing) || value > existing.Value)
            {
                results[exePath] = new TaskbarProgressEntry
                {
                    ExePath = exePath,
                    Value = value.Value,
                    WindowHandle = window.Hwnd
                };
            }
        }

        return results.Values.OrderByDescending(e => e.Value).ToList();
    }

    public static double? ParsePercent(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var match = PercentPattern.Match(text);
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out var percent)) return null;
        return Math.Clamp(percent / 100.0, 0, 1);
    }

    private static bool TryGetProcessPath(nint hwnd, out string exePath)
    {
        exePath = string.Empty;
        _ = GetWindowThreadProcessId(hwnd, out var pid);
        if (pid == 0) return false;
        try
        {
            using var process = System.Diagnostics.Process.GetProcessById((int)pid);
            exePath = process.MainModule?.FileName ?? string.Empty;
            return !string.IsNullOrWhiteSpace(exePath);
        }
        catch
        {
            return false;
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);
}
