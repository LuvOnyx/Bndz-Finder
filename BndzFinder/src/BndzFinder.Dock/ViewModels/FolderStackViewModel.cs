using BndzFinder.Core.Models;
using BndzFinder.Shell.Dock;
using BndzFinder.Shell.Icons;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BndzFinder.Dock.ViewModels;

public partial class FolderStackViewModel : ObservableObject
{
    private readonly FolderStackService _stacks;
    private readonly IIconPipeline _icons;

    [ObservableProperty] private bool _isOpen;
    [ObservableProperty] private FolderStackView _viewMode = FolderStackView.Automatic;
    [ObservableProperty] private FolderSortMode _sortMode = FolderSortMode.Name;
    [ObservableProperty] private IReadOnlyList<FolderStackEntry> _entries = [];
    [ObservableProperty] private string _folderPath = string.Empty;

    public FolderStackViewModel(
        string folderPath,
        FolderStackView view = FolderStackView.Automatic,
        FolderSortMode sort = FolderSortMode.Name,
        FolderStackService? stacks = null,
        IIconPipeline? icons = null)
    {
        FolderPath = folderPath;
        ViewMode = view;
        SortMode = sort;
        _stacks = stacks ?? new FolderStackService();
        _icons = icons ?? new MacStyleIconPipeline();
        Refresh();
        _ = LoadIconsAsync();
    }

    [RelayCommand]
    public void Toggle() => IsOpen = !IsOpen;

    [RelayCommand]
    public void SetView(FolderStackView view)
    {
        ViewMode = view;
        Refresh();
        _ = LoadIconsAsync();
    }

    [RelayCommand]
    public void SetSort(FolderSortMode sort)
    {
        SortMode = sort;
        Refresh();
        _ = LoadIconsAsync();
    }

    [RelayCommand]
    public async Task OpenEntryAsync(FolderStackEntry entry)
    {
        if (entry.IsDirectory)
        {
            FolderPath = entry.Path;
            Refresh();
            await LoadIconsAsync().ConfigureAwait(false);
            return;
        }
        if (OperatingSystem.IsWindows())
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(entry.Path) { UseShellExecute = true });
        }
    }

    public void Refresh()
    {
        var paths = _stacks.GetContents(FolderPath, SortMode);
        Entries = _stacks.BuildLayout(paths, ViewMode)
            .Select((e, i) => e with { Index = i })
            .ToList();
    }

    private async Task LoadIconsAsync()
    {
        var updated = new List<FolderStackEntry>(Entries.Count);
        foreach (var entry in Entries)
        {
            var iconPath = string.Empty;
            try
            {
                await _icons.ProcessAsync(entry.Path).ConfigureAwait(false);
                iconPath = _icons.GetCachePath(entry.Path);
            }
            catch
            {
                // Protected or transient paths can fail icon extraction.
            }

            updated.Add(entry with { IconCachePath = iconPath });
        }

        Entries = updated;
    }
}
