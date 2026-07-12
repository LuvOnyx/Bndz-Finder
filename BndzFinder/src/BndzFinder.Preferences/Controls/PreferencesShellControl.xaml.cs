using BndzFinder.Core.Models;
using BndzFinder.Preferences.Controls;
using BndzFinder.Preferences.Localization;
using BndzFinder.Preferences.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BndzFinder.Preferences.Controls;

public sealed partial class PreferencesShellControl : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(PreferencesViewModel), typeof(PreferencesShellControl),
            new PropertyMetadata(null, (_, e) => ((PreferencesShellControl)_).Bind()));

    public PreferencesViewModel? ViewModel
    {
        get => (PreferencesViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public PreferencesShellControl() => InitializeComponent();

    private void Bind()
    {
        if (ViewModel is null) return;
        SectionTitle.Text = Loc.Get("Preferences.Title");
        DockPositionBox.ItemsSource = Enum.GetValues<DockPosition>();
        DockPositionBox.SelectedItem = ViewModel.Settings.DockPosition;
        DisplayModeBox.ItemsSource = Enum.GetValues<DockDisplayMode>();
        DisplayModeBox.SelectedItem = ViewModel.Settings.DockDisplayMode;
        GlassEffectBox.ItemsSource = Enum.GetValues<GlassEffect>();
        GlassEffectBox.SelectedItem = ViewModel.Settings.DockGlassEffect;
        IconEffectBox.ItemsSource = Enum.GetValues<IconHoverEffect>();
        IconEffectBox.SelectedItem = ViewModel.Settings.IconEffect;
        MinimizeEffectBox.ItemsSource = Enum.GetValues<MinimizeEffect>();
        MinimizeEffectBox.SelectedItem = ViewModel.Settings.MinimizeEffect;
        LanguageBox.ItemsSource = Loc.SupportedLanguages;
        LanguageBox.SelectedItem = ViewModel.Settings.Language;
        DockMonitorBox.ItemsSource = ViewModel.AvailableMonitors;
        DockMonitorBox.SelectedItem = ViewModel.Settings.DockMonitorName;
        FinderMonitorBox.ItemsSource = ViewModel.AvailableMonitors;
        FinderMonitorBox.SelectedItem = ViewModel.Settings.FinderMonitorName;
        AccentPicker.SelectedHex = ViewModel.Settings.AccentColor;
        GlassTintPicker.SelectedHex = ViewModel.GlassTintColor;
        DockOpacitySlider.Value = ViewModel.Settings.DockOpacity * 100;
        CornerRadiusSlider.Value = ViewModel.Settings.DockCornerRadius;
        BindWidgetToggles();
        LaunchpadHotkeyBox.Text = FormatHotkey(ViewModel.Settings.LaunchpadHotkey);
        StageManagerHotkeyBox.Text = FormatHotkey(ViewModel.Settings.StageManagerHotkey);
    }

    private void BindWidgetToggles()
    {
        WidgetCpu.IsOn = ViewModel!.Settings.ShowCpu;
        WidgetGpu.IsOn = ViewModel.Settings.ShowGpu;
        WidgetMemory.IsOn = ViewModel.Settings.ShowMemory;
        WidgetDisk.IsOn = ViewModel.Settings.ShowDisk;
        WidgetNetwork.IsOn = ViewModel.Settings.ShowNetwork;
        WidgetBattery.IsOn = ViewModel.Settings.ShowBattery;
        WidgetWeather.IsOn = ViewModel.Settings.ShowWeather;
        WidgetAudio.IsOn = ViewModel.Settings.ShowAudio;
        WidgetBluetooth.IsOn = ViewModel.Settings.ShowBluetooth;
        WidgetDisplay.IsOn = ViewModel.Settings.ShowDisplay;
        WidgetKeyboard.IsOn = ViewModel.Settings.ShowKeyboard;
        WidgetMedia.IsOn = ViewModel.Settings.ShowMediaControl;
        WidgetNotifications.IsOn = ViewModel.Settings.ShowNotifications;
    }

    private void OnSectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item) return;
        var tag = item.Tag?.ToString() ?? "General";
        GeneralSection.Visibility = tag == "General" ? Visibility.Visible : Visibility.Collapsed;
        AppearanceSection.Visibility = tag == "Appearance" ? Visibility.Visible : Visibility.Collapsed;
        ScreenSection.Visibility = tag == "Screen" ? Visibility.Visible : Visibility.Collapsed;
        WidgetsSection.Visibility = tag == "SystemIconTray" ? Visibility.Visible : Visibility.Collapsed;
        LaunchpadSection.Visibility = tag == "Launchpad" ? Visibility.Visible : Visibility.Collapsed;
        WindowAnimationsSection.Visibility = tag == "WindowAnimations" ? Visibility.Visible : Visibility.Collapsed;
        AdvancedSection.Visibility = tag == "Advanced" ? Visibility.Visible : Visibility.Collapsed;
        ThemesSection.Visibility = tag == "Themes" ? Visibility.Visible : Visibility.Collapsed;
        SectionTitle.Text = Loc.Get($"Section.{tag}");
    }

    private async void OnSave(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null) return;
        if (DockPositionBox.SelectedItem is DockPosition position) ViewModel.Settings.DockPosition = position;
        if (DisplayModeBox.SelectedItem is DockDisplayMode mode) ViewModel.Settings.DockDisplayMode = mode;
        if (GlassEffectBox.SelectedItem is GlassEffect glass) ViewModel.Settings.DockGlassEffect = glass;
        if (IconEffectBox.SelectedItem is IconHoverEffect iconEffect) ViewModel.Settings.IconEffect = iconEffect;
        if (MinimizeEffectBox.SelectedItem is MinimizeEffect minimize) ViewModel.Settings.MinimizeEffect = minimize;
        if (LanguageBox.SelectedItem is string lang) ViewModel.Settings.Language = lang;
        ViewModel.Settings.DockMonitorName = DockMonitorBox.SelectedItem?.ToString();
        ViewModel.Settings.FinderMonitorName = FinderMonitorBox.SelectedItem?.ToString();
        ViewModel.Settings.AccentColor = AccentPicker.SelectedHex;
        ViewModel.Settings.DockOpacity = DockOpacitySlider.Value / 100;
        ViewModel.Settings.DockCornerRadius = CornerRadiusSlider.Value;
        ViewModel.SetGlassTintCommand.Execute(GlassTintPicker.SelectedHex);
        ViewModel.SetWidgetEnabledCommand.Execute(("cpu", WidgetCpu.IsOn));
        ViewModel.SetWidgetEnabledCommand.Execute(("gpu", WidgetGpu.IsOn));
        ViewModel.SetWidgetEnabledCommand.Execute(("memory", WidgetMemory.IsOn));
        ViewModel.SetWidgetEnabledCommand.Execute(("disk", WidgetDisk.IsOn));
        ViewModel.SetWidgetEnabledCommand.Execute(("network", WidgetNetwork.IsOn));
        ViewModel.SetWidgetEnabledCommand.Execute(("battery", WidgetBattery.IsOn));
        ViewModel.SetWidgetEnabledCommand.Execute(("weather", WidgetWeather.IsOn));
        ViewModel.SetWidgetEnabledCommand.Execute(("audio", WidgetAudio.IsOn));
        ViewModel.SetWidgetEnabledCommand.Execute(("bluetooth", WidgetBluetooth.IsOn));
        ViewModel.SetWidgetEnabledCommand.Execute(("display", WidgetDisplay.IsOn));
        ViewModel.SetWidgetEnabledCommand.Execute(("keyboard", WidgetKeyboard.IsOn));
        ViewModel.SetWidgetEnabledCommand.Execute(("media", WidgetMedia.IsOn));
        ViewModel.SetWidgetEnabledCommand.Execute(("notifications", WidgetNotifications.IsOn));
        await ViewModel.SaveCommand.ExecuteAsync(null);
    }

    private async void OnBackup(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null) return;
        await ViewModel.BackupCommand.ExecuteAsync(null);
    }

    private static string FormatHotkey(HotkeyBinding binding)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(binding.Modifiers)) parts.Add(binding.Modifiers);
        if (!string.IsNullOrWhiteSpace(binding.Key)) parts.Add(binding.Key);
        return parts.Count == 0 ? "(none)" : string.Join(" + ", parts);
    }
}
