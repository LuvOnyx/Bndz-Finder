using BndzFinder.Core.Models;
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
        SectionTitle.Text = "Dock Preferences";
        DockPositionBox.ItemsSource = Enum.GetValues<DockPosition>();
        DockPositionBox.SelectedItem = ViewModel.Settings.DockPosition;
        AutoHideDock.IsOn = ViewModel.Settings.DockDisplayMode == DockDisplayMode.AutoHide;
        GlassEffectBox.ItemsSource = Enum.GetValues<GlassEffect>();
        GlassEffectBox.SelectedItem = ViewModel.Settings.DockGlassEffect;
        IconEffectBox.ItemsSource = Enum.GetValues<IconHoverEffect>();
        IconEffectBox.SelectedItem = ViewModel.Settings.IconEffect;
        MinimizeEffectBox.ItemsSource = Enum.GetValues<MinimizeEffect>();
        MinimizeEffectBox.SelectedItem = ViewModel.Settings.MinimizeEffect;
        LaunchpadHotkeyBox.Text = FormatHotkey(ViewModel.Settings.LaunchpadHotkey);
        StageManagerHotkeyBox.Text = FormatHotkey(ViewModel.Settings.StageManagerHotkey);
    }

    private void OnSectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item) return;
        var tag = item.Tag?.ToString() ?? "General";
        GeneralSection.Visibility = tag == "General" ? Visibility.Visible : Visibility.Collapsed;
        AppearanceSection.Visibility = tag == "Appearance" ? Visibility.Visible : Visibility.Collapsed;
        WindowAnimationsSection.Visibility = tag == "WindowAnimations" ? Visibility.Visible : Visibility.Collapsed;
        AdvancedSection.Visibility = tag == "Advanced" ? Visibility.Visible : Visibility.Collapsed;
        SectionTitle.Text = item.Content?.ToString() ?? tag;
    }

    private async void OnSave(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null) return;
        if (DockPositionBox.SelectedItem is DockPosition position)
            ViewModel.Settings.DockPosition = position;
        if (GlassEffectBox.SelectedItem is GlassEffect glass)
            ViewModel.Settings.DockGlassEffect = glass;
        if (IconEffectBox.SelectedItem is IconHoverEffect iconEffect)
            ViewModel.Settings.IconEffect = iconEffect;
        if (MinimizeEffectBox.SelectedItem is MinimizeEffect minimize)
            ViewModel.Settings.MinimizeEffect = minimize;
        ViewModel.Settings.DockDisplayMode = AutoHideDock.IsOn ? DockDisplayMode.AutoHide : DockDisplayMode.Normal;
        await ViewModel.SaveCommand.ExecuteAsync(null);
    }

    private static string FormatHotkey(Core.Models.HotkeyBinding binding)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(binding.Modifiers)) parts.Add(binding.Modifiers);
        if (!string.IsNullOrWhiteSpace(binding.Key)) parts.Add(binding.Key);
        return parts.Count == 0 ? "(none)" : string.Join(" + ", parts);
    }
}
