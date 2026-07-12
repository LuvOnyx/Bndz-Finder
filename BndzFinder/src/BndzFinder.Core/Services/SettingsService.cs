using System.Text.Json;
using System.Text.Json.Serialization;
using BndzFinder.Core.Settings;

namespace BndzFinder.Core.Services;

public interface ISettingsService
{
    BndzFinderSettings Current { get; }
    string SettingsPath { get; }
    Task LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(CancellationToken cancellationToken = default);
    Task ReplaceCurrentAsync(BndzFinderSettings settings, CancellationToken cancellationToken = default);
    Task ResetComponentAsync(string component, CancellationToken cancellationToken = default);
    event EventHandler<BndzFinderSettings>? SettingsChanged;
}

public sealed class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private BndzFinderSettings _current = new();

    public BndzFinderSettings Current => _current;

    public string SettingsPath { get; }

    public event EventHandler<BndzFinderSettings>? SettingsChanged;

    public SettingsService(string? settingsPath = null)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var directory = Path.Combine(appData, "BndzFinder");
        Directory.CreateDirectory(directory);
        SettingsPath = settingsPath ?? Path.Combine(directory, "settings.json");
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(SettingsPath))
            {
                _current = new BndzFinderSettings();
                await SaveInternalAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            await using var stream = File.OpenRead(SettingsPath);
            var loaded = await JsonSerializer.DeserializeAsync<BndzFinderSettings>(stream, JsonOptions, cancellationToken)
                .ConfigureAwait(false);
            _current = loaded ?? new BndzFinderSettings();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await SaveInternalAsync(cancellationToken).ConfigureAwait(false);
            SettingsChanged?.Invoke(this, _current);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ReplaceCurrentAsync(BndzFinderSettings settings, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _current = settings;
            await SaveInternalAsync(cancellationToken).ConfigureAwait(false);
            SettingsChanged?.Invoke(this, _current);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ResetComponentAsync(string component, CancellationToken cancellationToken = default)
    {
        var defaults = new BndzFinderSettings();
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            switch (component.ToLowerInvariant())
            {
                case "dock":
                    _current.DockPosition = defaults.DockPosition;
                    _current.DockDisplayMode = defaults.DockDisplayMode;
                    _current.IconSize = defaults.IconSize;
                    _current.DockItems = [];
                    break;
                case "finder":
                    _current.FinderEnabled = defaults.FinderEnabled;
                    _current.FinderHeight = defaults.FinderHeight;
                    break;
                case "launchpad":
                    _current.LaunchpadEnabled = defaults.LaunchpadEnabled;
                    _current.LaunchpadBlacklist = [];
                    break;
                case "stagemanager":
                    _current.StageManagerEnabled = defaults.StageManagerEnabled;
                    _current.StageManagerBlacklist = [];
                    break;
                case "theme":
                    _current.GlobalBlurValue = defaults.GlobalBlurValue;
                    _current.ActiveDockSkin = defaults.ActiveDockSkin;
                    _current.ActiveIconTheme = defaults.ActiveIconTheme;
                    break;
                default:
                    _current = new BndzFinderSettings();
                    break;
            }

            await SaveInternalAsync(cancellationToken).ConfigureAwait(false);
            SettingsChanged?.Invoke(this, _current);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task SaveInternalAsync(CancellationToken cancellationToken)
    {
        var tempPath = SettingsPath + ".tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, _current, JsonOptions, cancellationToken).ConfigureAwait(false);
        }

        if (File.Exists(SettingsPath))
        {
            File.Replace(tempPath, SettingsPath, SettingsPath + ".bak");
        }
        else
        {
            File.Move(tempPath, SettingsPath);
        }
    }
}
