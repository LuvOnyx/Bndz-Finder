using System.Text.Json;
using BndzFinder.Core.Models;

namespace BndzFinder.Core.Services;

public static class ShellIpcParsers
{
    public static IReadOnlyList<TrayIconSnapshot> ParseTrayIcons(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<TrayIconSnapshot>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public static IReadOnlyList<WindowSnapshot> ParseWindowList(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<WindowSnapshot>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }
}

public sealed class TrayIconSnapshot
{
    public string Tooltip { get; init; } = string.Empty;
    public uint IconId { get; init; }
    public long OwnerWindow { get; init; }
    public string? IconDataBase64 { get; init; }
}

public sealed class WindowSnapshot
{
    public string Title { get; init; } = string.Empty;
    public long Handle { get; init; }
}

public sealed class MinimizeStartedInfo
{
    public long Handle { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public string Effect { get; init; } = "Genie";
    public string? SnapshotBase64 { get; init; }
}

public static class MinimizeStartedPayload
{
    public static string Serialize(long hwnd, int x, int y, int width, int height, string effect, string? snapshotBase64) =>
        JsonSerializer.Serialize(new
        {
            Handle = hwnd,
            X = x,
            Y = y,
            Width = width,
            Height = height,
            Effect = effect,
            SnapshotBase64 = snapshotBase64
        });

    public static MinimizeStartedInfo? Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<MinimizeStartedInfo>(json); }
        catch { return null; }
    }
}
