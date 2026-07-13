namespace BndzFinder.Core.Services;

/// <summary>
/// WinUI host HWND for file pickers and modal dialogs (set by BndzFinder.App at startup).
/// </summary>
public static class UiHostContext
{
    public static Func<nint>? GetOwnerWindowHandle { get; set; }
}
