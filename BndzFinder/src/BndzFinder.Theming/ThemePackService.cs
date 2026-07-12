using System.IO.Compression;
using System.Text.Json;

namespace BndzFinder.Theming;

public sealed class ThemePackManifest
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Author { get; init; }
    public string? Version { get; init; }
    public string? DockSkin { get; init; }
    public string? IconShell { get; init; }
    public string? ProgressBar { get; init; }
    public string? RunIndicator { get; init; }
    public string? Delimiter { get; init; }
    public string? TimeSkin { get; init; }
    public string? CalendarSkin { get; init; }
}

public interface IThemePackService
{
    Task<ThemePackManifest> ImportAsync(string zipPath, CancellationToken cancellationToken = default);
    Task ExportAsync(ThemePackManifest manifest, string sourceDirectory, string zipPath, CancellationToken cancellationToken = default);
    IReadOnlyList<ThemePackManifest> ListInstalled();
}

public sealed class ThemePackService : IThemePackService
{
    private readonly string _themesDirectory;

    public ThemePackService(string? themesDirectory = null)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _themesDirectory = themesDirectory ?? Path.Combine(appData, "BndzFinder", "themes");
        Directory.CreateDirectory(_themesDirectory);
    }

    public async Task<ThemePackManifest> ImportAsync(string zipPath, CancellationToken cancellationToken = default)
    {
        var extractDir = Path.Combine(_themesDirectory, Path.GetFileNameWithoutExtension(zipPath));
        if (Directory.Exists(extractDir))
        {
            Directory.Delete(extractDir, recursive: true);
        }
        Directory.CreateDirectory(extractDir);
        await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, extractDir), cancellationToken).ConfigureAwait(false);

        var manifestPath = Path.Combine(extractDir, "manifest.json");
        await using var stream = File.OpenRead(manifestPath);
        var manifest = await JsonSerializer.DeserializeAsync<ThemePackManifest>(stream, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return manifest ?? throw new InvalidDataException("Invalid theme pack manifest.");
    }

    public async Task ExportAsync(ThemePackManifest manifest, string sourceDirectory, string zipPath, CancellationToken cancellationToken = default)
    {
        var tempManifest = Path.Combine(sourceDirectory, "manifest.json");
        await File.WriteAllTextAsync(tempManifest, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }), cancellationToken).ConfigureAwait(false);
        if (File.Exists(zipPath)) File.Delete(zipPath);
        await Task.Run(() => ZipFile.CreateFromDirectory(sourceDirectory, zipPath), cancellationToken).ConfigureAwait(false);
    }

    public IReadOnlyList<ThemePackManifest> ListInstalled()
    {
        var results = new List<ThemePackManifest>();
        foreach (var dir in Directory.EnumerateDirectories(_themesDirectory))
        {
            var manifestPath = Path.Combine(dir, "manifest.json");
            if (!File.Exists(manifestPath)) continue;
            var json = File.ReadAllText(manifestPath);
            var manifest = JsonSerializer.Deserialize<ThemePackManifest>(json);
            if (manifest is not null) results.Add(manifest);
        }
        return results;
    }
}
