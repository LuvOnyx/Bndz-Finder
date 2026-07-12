using BndzFinder.Core.Services;
using BndzFinder.Interop;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BndzFinder.StageManager.ViewModels;

public partial class StageManagerViewModel : ObservableObject
{
    private readonly ISettingsService _settings;

    [ObservableProperty] private IReadOnlyList<WindowThumbnailItem> _windows = [];

    public int ThumbnailSize => _settings.Current.StageManagerWindowSize;
    public bool ShowTitles => _settings.Current.ShowStageManagerWindowTitle;
    public bool UseBlur => _settings.Current.StageManagerWindowBlur;

    public StageManagerViewModel(ISettingsService settings)
    {
        _settings = settings;
        _ = PollWindowsAsync();
    }

    private async Task PollWindowsAsync()
    {
        while (true)
        {
            Windows = WindowEnumerationService.GetOpenWindows(_settings.Current.StageManagerBlacklist)
                .Take(_settings.Current.StageManagerWindowCount)
                .ToList();
            await Task.Delay(500).ConfigureAwait(false);
        }
    }

    [RelayCommand]
    public void FocusWindow(WindowThumbnailItem item) => WindowOperations.FocusWindow(item.Hwnd);

    [RelayCommand]
    public void CloseWindow(WindowThumbnailItem item) => WindowOperations.CloseWindow(item.Hwnd);
}
