using BndzFinder.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BndzFinder.Launchpad.ViewModels;

public partial class LaunchpadViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly AppCatalogService _catalog;

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial IReadOnlyList<AppCatalogEntry> AllApps { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<AppCatalogEntry> FilteredApps { get; set; } = [];

    [ObservableProperty]
    public partial int CurrentPage { get; set; }

    [ObservableProperty]
    public partial bool IsOverlayVisible { get; set; }

    public int PageSize => 35;
    public int IconSize => _settings.Current.LaunchpadIconSize;
    public bool HideLabels => _settings.Current.LaunchpadHideLabels;

    public LaunchpadViewModel(ISettingsService settings, AppCatalogService? catalog = null)
    {
        _settings = settings;
        _catalog = catalog ?? new AppCatalogService();
        _ = LoadAsync();
    }

    partial void OnSearchQueryChanged(string value)
    {
        CurrentPage = 0;
        ApplyFilter();
    }

    [RelayCommand]
    public void NextPage()
    {
        if ((CurrentPage + 1) * PageSize < FilteredApps.Count)
            CurrentPage++;
    }

    [RelayCommand]
    public void PreviousPage()
    {
        if (CurrentPage > 0) CurrentPage--;
    }

    [RelayCommand]
    public void Dismiss() => IsOverlayVisible = false;

    private async Task LoadAsync()
    {
        AllApps = await _catalog.LoadAsync(_settings.Current.LaunchpadIconSource).ConfigureAwait(false);
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var blacklist = _settings.Current.LaunchpadBlacklist.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var query = SearchQuery.Trim();
        var filtered = AllApps
            .Where(a => !blacklist.Contains(a.Id))
            .Where(a => string.IsNullOrEmpty(query) || a.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.Name)
            .ToList();
        FilteredApps = filtered.Skip(CurrentPage * PageSize).Take(PageSize).ToList();
    }

    [RelayCommand]
    public void Launch(AppCatalogEntry entry)
    {
        if (!OperatingSystem.IsWindows()) return;
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(entry.Path) { UseShellExecute = true });
        IsOverlayVisible = false;
    }
}

public sealed class AppCatalogEntry
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Path { get; init; }
    public string? IconPath { get; init; }
}

public sealed class AppCatalogService
{
    public Task<IReadOnlyList<AppCatalogEntry>> LoadAsync(string source)
    {
        if (!OperatingSystem.IsWindows())
            return Task.FromResult<IReadOnlyList<AppCatalogEntry>>([]);

        var results = new List<AppCatalogEntry>();
        if (source is "desktop" or "startmenu" or "both")
        {
            if (source is not "startmenu")
                results.AddRange(ScanFolder(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)));
            if (source is not "desktop")
            {
                var startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
                results.AddRange(ScanFolder(startMenu));
                var commonStart = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs");
                results.AddRange(ScanFolder(commonStart));
            }
        }
        return Task.FromResult<IReadOnlyList<AppCatalogEntry>>(results.DistinctBy(r => r.Id).ToList());
    }

    private static IEnumerable<AppCatalogEntry> ScanFolder(string folder)
    {
        if (!Directory.Exists(folder)) yield break;
        foreach (var file in Directory.EnumerateFiles(folder, "*.lnk", SearchOption.AllDirectories))
        {
            yield return new AppCatalogEntry
            {
                Id = file,
                Name = Path.GetFileNameWithoutExtension(file),
                Path = file
            };
        }
        foreach (var file in Directory.EnumerateFiles(folder, "*.exe", SearchOption.TopDirectoryOnly))
        {
            yield return new AppCatalogEntry
            {
                Id = file,
                Name = Path.GetFileNameWithoutExtension(file),
                Path = file
            };
        }
    }
}
