using BndzFinder.Core.Models;
using BndzFinder.Core.Services;
using BndzFinder.Preferences.Localization;
using BndzFinder.Theming;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BndzFinder.Preferences.ViewModels;

public enum PreferencesSection
{
    General,
    Appearance,
    SystemIconTray,
    Screen,
    LookAndBehavior,
    Launchpad,
    WindowAnimations,
    AudioDisplayNetwork,
    Advanced,
    Themes
}

public partial class PreferencesViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IThemePackService _themes;
    private readonly IThemePackResolver _themeResolver;
    private readonly IMacAppearanceService _appearance;
    private readonly IBackupService _backup;
    private readonly IDisplayMonitorService _monitors;

    [ObservableProperty] private PreferencesSection _selectedSection = PreferencesSection.General;
    [ObservableProperty] private string _glassTintColor = "#101010";
    [ObservableProperty] private IReadOnlyList<string> _availableMonitors = [];
    [ObservableProperty] private IReadOnlyList<ThemePackManifest> _installedThemes = [];
    [ObservableProperty] private ThemePackManifest? _selectedTheme;
    [ObservableProperty] private IReadOnlyList<string> _availableWallpapers = [];
    [ObservableProperty] private string? _selectedWallpaper;

    public BndzFinder.Core.Settings.BndzFinderSettings Settings => _settings.Current;

    public PreferencesViewModel(
        ISettingsService settings,
        IThemePackService? themes = null,
        IThemePackResolver? themeResolver = null,
        IMacAppearanceService? appearance = null,
        IBackupService? backup = null,
        IDisplayMonitorService? monitors = null)
    {
        _settings = settings;
        _themes = themes ?? new ThemePackService();
        _themeResolver = themeResolver ?? new ThemePackResolver(_themes);
        _appearance = appearance ?? new MacAppearanceService(_themeResolver);
        _backup = backup ?? new BackupService(settings);
        _monitors = monitors ?? new DisplayMonitorService();
        AvailableMonitors = _monitors.GetMonitors().Select(m => m.Name).ToList();
        GlassTintColor = settings.Current.LiquidGlass?.TintColor ?? "#101010";
        RefreshThemes();
    }

    public void RefreshThemes()
    {
        InstalledThemes = _themes.ListInstalled();
        SelectedTheme = InstalledThemes.FirstOrDefault(t =>
                             t.Id.Equals(Settings.ActiveDockSkin ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                             || t.Name.Equals(Settings.ActiveDockSkin ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                         ?? InstalledThemes.FirstOrDefault();
        AvailableWallpapers = SelectedTheme is null
            ? []
            : _themeResolver.ListWallpapers(Settings);
        SelectedWallpaper = Settings.ActiveWallpaper ?? SelectedTheme?.Wallpaper;
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        if (Settings.LiquidGlass is not null)
            Settings.LiquidGlass.TintColor = GlassTintColor;
        Loc.Language = Settings.Language;
        await _settings.SaveAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    public async Task ResetSectionAsync(string component) =>
        await _settings.ResetComponentAsync(component).ConfigureAwait(false);

    [RelayCommand]
    public async Task ImportThemeAsync(string zipPath)
    {
        var manifest = await _themes.ImportAsync(zipPath).ConfigureAwait(false);
        RefreshThemes();
        SelectedTheme = manifest;
    }

    [RelayCommand]
    public async Task ApplyThemeAsync()
    {
        if (SelectedTheme is null) return;

        if (!string.IsNullOrWhiteSpace(SelectedWallpaper))
            Settings.ActiveWallpaper = SelectedWallpaper;

        _appearance.ApplyTheme(SelectedTheme, Settings);
        await _settings.SaveAsync().ConfigureAwait(false);
        RefreshThemes();
    }

    [RelayCommand]
    public async Task BackupAsync() => _ = await _backup.BackupAsync().ConfigureAwait(false);

    [RelayCommand]
    public async Task RestoreAsync(string path) => await _backup.RestoreAsync(path).ConfigureAwait(false);

    [RelayCommand]
    public void SetDockPosition(DockPosition position) => Settings.DockPosition = position;

    [RelayCommand]
    public void SetDockDisplayMode(DockDisplayMode mode) => Settings.DockDisplayMode = mode;

    [RelayCommand]
    public void SetMinimizeEffect(MinimizeEffect effect) => Settings.MinimizeEffect = effect;

    public void SetWidgetEnabled(string widget, bool enabled)
    {
        switch (widget)
        {
            case "cpu": Settings.ShowCpu = enabled; break;
            case "gpu": Settings.ShowGpu = enabled; break;
            case "memory": Settings.ShowMemory = enabled; break;
            case "disk": Settings.ShowDisk = enabled; break;
            case "network": Settings.ShowNetwork = enabled; break;
            case "battery": Settings.ShowBattery = enabled; break;
            case "weather": Settings.ShowWeather = enabled; break;
            case "audio": Settings.ShowAudio = enabled; break;
            case "bluetooth": Settings.ShowBluetooth = enabled; break;
            case "display": Settings.ShowDisplay = enabled; break;
            case "keyboard": Settings.ShowKeyboard = enabled; break;
            case "media": Settings.ShowMediaControl = enabled; break;
            case "notifications": Settings.ShowNotifications = enabled; break;
            case "microphone": Settings.ShowMicrophone = enabled; break;
            case "lyrics": Settings.ShowLyrics = enabled; break;
        }
        OnPropertyChanged(nameof(Settings));
    }

    [RelayCommand]
    public void SetAccentColor(string hex) => Settings.AccentColor = hex;

    [RelayCommand]
    public void SetGlassTint(string hex) => GlassTintColor = hex;

    [RelayCommand]
    public void SetLanguage(string language) => Settings.Language = language;

    public void SetHotkey(string hotkeyId, string? modifiers, string? key)
    {
        var binding = hotkeyId switch
        {
            "dock" => Settings.DockHotkey,
            "finder" => Settings.FinderHotkey,
            "launchpad" => Settings.LaunchpadHotkey,
            "stage" => Settings.StageManagerHotkey,
            _ => null
        };
        if (binding is null) return;
        binding.Modifiers = modifiers;
        binding.Key = key;
        OnPropertyChanged(nameof(Settings));
    }

    partial void OnSelectedThemeChanged(ThemePackManifest? value)
    {
        if (value is null) return;
        AvailableWallpapers = _themeResolver.ListWallpapers(Settings);
        SelectedWallpaper = Settings.ActiveWallpaper ?? value.Wallpaper;
    }
}
