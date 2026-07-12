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
        ScreenRoundToggle.IsOn = ViewModel.Settings.ScreenRoundEnabled;
        ScreenRoundRadiusSlider.Value = ViewModel.Settings.ScreenRoundRadius;
        AccentPicker.SelectedHex = ViewModel.Settings.AccentColor;
        GlassTintPicker.SelectedHex = ViewModel.GlassTintColor;
        DockOpacitySlider.Value = ViewModel.Settings.DockOpacity * 100;
        CornerRadiusSlider.Value = ViewModel.Settings.DockCornerRadius;
        BindWidgetToggles();
        DockHotkeyBox.Text = FormatHotkey(ViewModel.Settings.DockHotkey);
        FinderHotkeyBox.Text = FormatHotkey(ViewModel.Settings.FinderHotkey);
        LaunchpadHotkeyBox.Text = FormatHotkey(ViewModel.Settings.LaunchpadHotkey);
        StageManagerHotkeyBox.Text = FormatHotkey(ViewModel.Settings.StageManagerHotkey);
        HotCornerLeftBox.ItemsSource = Enum.GetValues<HotCornerAction>();
        HotCornerRightBox.ItemsSource = Enum.GetValues<HotCornerAction>();
        HotCornerLeftBox.SelectedItem = ViewModel.Settings.HotCorners.BottomLeft;
        HotCornerRightBox.SelectedItem = ViewModel.Settings.HotCorners.BottomRight;
        HideTaskbarToggle.IsOn = ViewModel.Settings.HideTaskbarWhenDockShown;
        HideTaskbarAllMonitorsToggle.IsOn = ViewModel.Settings.HideTaskbarAllMonitors;
        AutoHideTaskbarToggle.IsOn = ViewModel.Settings.AutoHideTaskbarAtStartup;
        PreviewOnToggle.IsOn = ViewModel.Settings.PreviewOn;
        PreviewSizeSlider.Value = ViewModel.Settings.PreviewSize;
        PreviewDelaySlider.Value = ViewModel.Settings.PreviewDelayMs;
        HideDockDelaySlider.Value = ViewModel.Settings.HideDockDelayMs;
        LockIconsToggle.IsOn = ViewModel.Settings.LockIcons;
        EdgeActivationToggle.IsOn = ViewModel.Settings.ShowDockActivationMouse;
        LaunchpadIconSizeSlider.Value = ViewModel.Settings.LaunchpadIconSize;
        LaunchpadHideLabelsToggle.IsOn = ViewModel.Settings.LaunchpadHideLabels;
        LaunchpadSourceBox.ItemsSource = new[] { "startmenu", "desktop", "both" };
        LaunchpadSourceBox.SelectedItem = ViewModel.Settings.LaunchpadIconSource;
        AudioPanelToggle.IsOn = ViewModel.Settings.ShowAudio;
        DisplayPanelToggle.IsOn = ViewModel.Settings.ShowDisplay;
        NetworkPanelToggle.IsOn = ViewModel.Settings.ShowNetwork;
        MicrophonePanelToggle.IsOn = ViewModel.Settings.ShowMicrophone;
        TrayWaitSlider.Value = ViewModel.Settings.TrayIconWaitTimeMs;
        AlwaysShowTrayToggle.IsOn = ViewModel.Settings.AlwaysShowAllTrayIcons;
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
        LookAndBehaviorSection.Visibility = tag == "LookAndBehavior" ? Visibility.Visible : Visibility.Collapsed;
        WidgetsSection.Visibility = tag == "SystemIconTray" ? Visibility.Visible : Visibility.Collapsed;
        LaunchpadSection.Visibility = tag == "Launchpad" ? Visibility.Visible : Visibility.Collapsed;
        AudioDisplayNetworkSection.Visibility = tag == "AudioDisplayNetwork" ? Visibility.Visible : Visibility.Collapsed;
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
        ViewModel.Settings.ScreenRoundEnabled = ScreenRoundToggle.IsOn;
        ViewModel.Settings.ScreenRoundRadius = (int)ScreenRoundRadiusSlider.Value;
        ViewModel.Settings.AccentColor = AccentPicker.SelectedHex;
        ViewModel.Settings.DockOpacity = DockOpacitySlider.Value / 100;
        ViewModel.Settings.DockCornerRadius = CornerRadiusSlider.Value;
        ViewModel.SetGlassTint(GlassTintPicker.SelectedHex);
        ViewModel.SetWidgetEnabled("cpu", WidgetCpu.IsOn);
        ViewModel.SetWidgetEnabled("gpu", WidgetGpu.IsOn);
        ViewModel.SetWidgetEnabled("memory", WidgetMemory.IsOn);
        ViewModel.SetWidgetEnabled("disk", WidgetDisk.IsOn);
        ViewModel.SetWidgetEnabled("network", WidgetNetwork.IsOn);
        ViewModel.SetWidgetEnabled("battery", WidgetBattery.IsOn);
        ViewModel.SetWidgetEnabled("weather", WidgetWeather.IsOn);
        ViewModel.SetWidgetEnabled("audio", WidgetAudio.IsOn);
        ViewModel.SetWidgetEnabled("bluetooth", WidgetBluetooth.IsOn);
        ViewModel.SetWidgetEnabled("display", WidgetDisplay.IsOn);
        ViewModel.SetWidgetEnabled("keyboard", WidgetKeyboard.IsOn);
        ViewModel.SetWidgetEnabled("media", WidgetMedia.IsOn);
        ViewModel.SetWidgetEnabled("notifications", WidgetNotifications.IsOn);
        ViewModel.Settings.HideTaskbarWhenDockShown = HideTaskbarToggle.IsOn;
        ViewModel.Settings.HideTaskbarAllMonitors = HideTaskbarAllMonitorsToggle.IsOn;
        ViewModel.Settings.AutoHideTaskbarAtStartup = AutoHideTaskbarToggle.IsOn;
        ViewModel.Settings.PreviewOn = PreviewOnToggle.IsOn;
        ViewModel.Settings.PreviewSize = (int)PreviewSizeSlider.Value;
        ViewModel.Settings.PreviewDelayMs = (int)PreviewDelaySlider.Value;
        ViewModel.Settings.HideDockDelayMs = (int)HideDockDelaySlider.Value;
        ViewModel.Settings.LockIcons = LockIconsToggle.IsOn;
        ViewModel.Settings.ShowDockActivationMouse = EdgeActivationToggle.IsOn;
        ViewModel.Settings.LaunchpadIconSize = (int)LaunchpadIconSizeSlider.Value;
        ViewModel.Settings.LaunchpadHideLabels = LaunchpadHideLabelsToggle.IsOn;
        if (LaunchpadSourceBox.SelectedItem is string source) ViewModel.Settings.LaunchpadIconSource = source;
        ViewModel.Settings.ShowAudio = AudioPanelToggle.IsOn;
        ViewModel.Settings.ShowDisplay = DisplayPanelToggle.IsOn;
        ViewModel.Settings.ShowNetwork = NetworkPanelToggle.IsOn;
        ViewModel.Settings.ShowMicrophone = MicrophonePanelToggle.IsOn;
        ViewModel.Settings.TrayIconWaitTimeMs = (int)TrayWaitSlider.Value;
        ViewModel.Settings.AlwaysShowAllTrayIcons = AlwaysShowTrayToggle.IsOn;
        if (HotCornerLeftBox.SelectedItem is HotCornerAction left) ViewModel.Settings.HotCorners.BottomLeft = left;
        if (HotCornerRightBox.SelectedItem is HotCornerAction right) ViewModel.Settings.HotCorners.BottomRight = right;
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
