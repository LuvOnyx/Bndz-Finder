using BndzFinder.Core.Services;
using BndzFinder.Interop;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BndzFinder.StageManager.ViewModels;

public partial class StageManagerViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IWindowCaptureService? _capture;
    private CancellationTokenSource? _pollCts;

    [ObservableProperty] private IReadOnlyList<WindowThumbnailItem> _windows = [];

    public int ThumbnailSize => _settings.Current.StageManagerWindowSize;
    public bool ShowTitles => _settings.Current.ShowStageManagerWindowTitle;
    public bool UseBlur => _settings.Current.StageManagerWindowBlur;
    public int ThumbnailSpacing => _settings.Current.StageManagerWindowSpace;

    public StageManagerViewModel(ISettingsService settings, IWindowCaptureService? capture = null)
    {
        _settings = settings;
        _capture = capture;
        _pollCts = new CancellationTokenSource();
        _ = PollWindowsAsync(_pollCts.Token);
    }

    public void ApplyWindowsFromPayload(string json)
    {
        var snapshots = ShellIpcParsers.ParseWindowList(json);
        if (snapshots.Count == 0) return;
        Windows = snapshots
            .Take(_settings.Current.StageManagerWindowCount)
            .Select(s => CreateThumbnailItem(s.Handle, s.Title))
            .ToList();
    }

    private WindowThumbnailItem CreateThumbnailItem(long handle, string title)
    {
        var hwnd = (nint)handle;
        byte[]? thumb = null;
        var width = 0;
        var height = 0;
        if (_capture?.CaptureWindow(hwnd) is { } capture)
        {
            thumb = capture.Pixels;
            width = capture.Width;
            height = capture.Height;
        }
        return new WindowThumbnailItem
        {
            Hwnd = hwnd,
            Title = title,
            Thumbnail = thumb,
            ThumbnailWidth = width,
            ThumbnailHeight = height
        };
    }

    private async Task PollWindowsAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            Windows = WindowEnumerationService.GetOpenWindows(_settings.Current.StageManagerBlacklist)
                .Take(_settings.Current.StageManagerWindowCount)
                .Select(w => CreateThumbnailItem(w.Hwnd, w.Title))
                .ToList();
            await Task.Delay(500, ct).ConfigureAwait(false);
        }
    }

    [RelayCommand]
    public void FocusWindow(WindowThumbnailItem item) => WindowOperations.FocusWindow(item.Hwnd);

    [RelayCommand]
    public void CloseWindow(WindowThumbnailItem item) => WindowOperations.CloseWindow(item.Hwnd);
}
