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

    [ObservableProperty]
    public partial bool IsOpen { get; set; }

    [ObservableProperty]
    public partial FolderStackView ViewMode { get; set; } = FolderStackView.Automatic;

    [ObservableProperty]
    public partial FolderSortMode SortMode { get; set; } = FolderSortMode.Name;

    [ObservableProperty]
    public partial IReadOnlyList<FolderStackEntry> Entries { get; set; } = [];

    [ObservableProperty]
    public partial string FolderPath { get; set; } = string.Empty;

    public FolderStackViewModel(string folderPath, FolderStackService? stacks = null, IIconPipeline? icons = null)
    {
        FolderPath = folderPath;
        _stacks = stacks ?? new FolderStackService();
        _icons = icons ?? new MacStyleIconPipeline();
        Refresh();
    }

    [RelayCommand]
    public void Toggle() => IsOpen = !IsOpen;

    [RelayCommand]
    public void SetView(FolderStackView view)
    {
        ViewMode = view;
        Refresh();
    }

    [RelayCommand]
    public void SetSort(FolderSortMode sort)
    {
        SortMode = sort;
        Refresh();
    }

    [RelayCommand]
    public async Task OpenEntryAsync(FolderStackEntry entry)
    {
        if (entry.IsDirectory)
        {
            FolderPath = entry.Path;
            Refresh();
            return;
        }
        if (OperatingSystem.IsWindows())
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(entry.Path) { UseShellExecute = true });
        }
        await Task.CompletedTask;
    }

    public void Refresh()
    {
        var paths = _stacks.GetContents(FolderPath, SortMode);
        Entries = _stacks.BuildLayout(paths, ViewMode)
            .Select((e, i) => e with { Index = i })
            .ToList();
    }
}
