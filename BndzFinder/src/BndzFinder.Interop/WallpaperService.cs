using System.Runtime.InteropServices;

namespace BndzFinder.Interop;

public interface IWallpaperService
{
    bool SetWallpaper(string imagePath);
    string? GetCurrentWallpaper();
}

/// <summary>
/// Applies desktop wallpaper via the Windows SystemParametersInfo API.
/// </summary>
public sealed class WindowsWallpaperService : IWallpaperService
{
    private const int SpiSetDesktopWallpaper = 0x0014;
    private const int SpiGetDesktopWallpaper = 0x0073;
    private const int SpifUpdateIniFile = 0x01;
    private const int SpifSendChange = 0x02;

    public bool SetWallpaper(string imagePath)
    {
        if (!OperatingSystem.IsWindows()) return false;
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath)) return false;

        return SystemParametersInfo(SpiSetDesktopWallpaper, 0, imagePath, SpifUpdateIniFile | SpifSendChange);
    }

    public string? GetCurrentWallpaper()
    {
        if (!OperatingSystem.IsWindows()) return null;

        var buffer = new char[260];
        if (!SystemParametersInfo(SpiGetDesktopWallpaper, buffer.Length, buffer, 0))
            return null;

        var path = new string(buffer).TrimEnd('\0');
        return string.IsNullOrWhiteSpace(path) ? null : path;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SystemParametersInfo(int uAction, int uParam, char[] lpvParam, int fuWinIni);
}
