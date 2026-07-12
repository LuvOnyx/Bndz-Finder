using BndzFinder.Core.Services;
using BndzFinder.Interop;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BndzFinder.StageManager.ViewModels;

public partial class StageManagerViewModel : ObservableObject
{
    private readonly ISettingsService _settings;

    [ObservableProperty] private IReadOnlyList<WindowThumbnailItem> _windows = [];

    public StageManagerViewModel(ISettingsService settings)
    {
        _settings = settings;
        _ = PollWindowsAsync();
    }

    private async Task PollWindowsAsync()
    {
        while (true)
        {
            Windows = WindowEnumerationService.GetOpenWindows(_settings.Current.StageManagerBlacklist);
            await Task.Delay(500).ConfigureAwait(false);
        }
    }

    [RelayCommand]
    public void FocusWindow(WindowThumbnailItem item)
    {
        if (!OperatingSystem.IsWindows()) return;
        _ = item.Hwnd;
    }

    [RelayCommand]
    public void CloseWindow(WindowThumbnailItem item)
    {
        if (!OperatingSystem.IsWindows()) return;
        _ = item.Hwnd;
    }
}
