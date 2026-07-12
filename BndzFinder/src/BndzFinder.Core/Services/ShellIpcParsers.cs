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
