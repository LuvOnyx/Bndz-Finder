using BndzFinder.Core.Models;
using BndzFinder.Core.Services;
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

    [ObservableProperty] private PreferencesSection _selectedSection = PreferencesSection.General;

    public BndzFinder.Core.Settings.BndzFinderSettings Settings => _settings.Current;

    public PreferencesViewModel(ISettingsService settings, IThemePackService? themes = null)
    {
        _settings = settings;
        _themes = themes ?? new ThemePackService();
    }

    [RelayCommand]
    public async Task SaveAsync() => await _settings.SaveAsync().ConfigureAwait(false);

    [RelayCommand]
    public async Task ResetSectionAsync(string component) =>
        await _settings.ResetComponentAsync(component).ConfigureAwait(false);

    [RelayCommand]
    public async Task ImportThemeAsync(string zipPath) =>
        await _themes.ImportAsync(zipPath).ConfigureAwait(false);

    [RelayCommand]
    public void SetDockPosition(DockPosition position)
    {
        Settings.DockPosition = position;
        OnPropertyChanged(nameof(Settings));
    }

    [RelayCommand]
    public void SetMinimizeEffect(MinimizeEffect effect)
    {
        Settings.MinimizeEffect = effect;
        OnPropertyChanged(nameof(Settings));
    }
}
