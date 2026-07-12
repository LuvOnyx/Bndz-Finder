using BndzFinder.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BndzFinder.Launchpad.ViewModels;

public partial class LaunchpadViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly AppCatalogService _catalog;

    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private IReadOnlyList<AppCatalogEntry> _allApps = [];
    [ObservableProperty] private IReadOnlyList<AppCatalogEntry> _filteredApps = [];

    public LaunchpadViewModel(ISettingsService settings, AppCatalogService? catalog = null)
    {
        _settings = settings;
        _catalog = catalog ?? new AppCatalogService();
        _ = LoadAsync();
    }

    partial void OnSearchQueryChanged(string value) => ApplyFilter();

    private async Task LoadAsync()
    {
        AllApps = await _catalog.LoadAsync(_settings.Current.LaunchpadIconSource).ConfigureAwait(false);
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var blacklist = _settings.Current.LaunchpadBlacklist.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var query = SearchQuery.Trim();
        FilteredApps = AllApps
            .Where(a => !blacklist.Contains(a.Id))
            .Where(a => string.IsNullOrEmpty(query) || a.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.Name)
            .ToList();
    }

    [RelayCommand]
    public void Launch(AppCatalogEntry entry)
    {
        if (!OperatingSystem.IsWindows()) return;
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(entry.Path) { UseShellExecute = true });
    }
}

public sealed class AppCatalogEntry
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Path { get; init; }
}

public sealed class AppCatalogService
{
    public Task<IReadOnlyList<AppCatalogEntry>> LoadAsync(string source)
    {
        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult<IReadOnlyList<AppCatalogEntry>>([]);
        }

        var results = new List<AppCatalogEntry>();
        if (source is "desktop" or "startmenu")
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (Directory.Exists(desktop))
            {
                foreach (var file in Directory.EnumerateFiles(desktop, "*.lnk"))
                {
                    results.Add(new AppCatalogEntry
                    {
                        Id = file,
                        Name = Path.GetFileNameWithoutExtension(file),
                        Path = file
                    });
                }
            }
        }
        return Task.FromResult<IReadOnlyList<AppCatalogEntry>>(results);
    }
}
