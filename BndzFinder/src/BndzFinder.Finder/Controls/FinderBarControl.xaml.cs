using BndzFinder.Finder.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace BndzFinder.Finder.Controls;

public sealed partial class FinderBarControl : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(FinderViewModel), typeof(FinderBarControl),
            new PropertyMetadata(null, OnViewModelChanged));

    public FinderViewModel? ViewModel
    {
        get => (FinderViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public FinderBarControl()
    {
        InitializeComponent();
        Resources["FinderWidgetButton"] = CreateWidgetButtonStyle();
    }

    private static Style CreateWidgetButtonStyle()
    {
        var style = new Style { TargetType = typeof(Button) };
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(6, 2, 6, 2)));
        style.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0))));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        style.Setters.Add(new Setter(Control.ForegroundProperty, new SolidColorBrush(Windows.UI.Color.FromArgb(230, 255, 255, 255))));
        style.Setters.Add(new Setter(Control.CornerRadiusProperty, new CornerRadius(6)));
        return style;
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FinderBarControl control) control.Bind();
    }

    private void Bind()
    {
        if (ViewModel is null) return;
        ViewModel.PropertyChanged += (_, args) => UpdateUI(args.PropertyName);
        Height = ViewModel.BarHeight;
        UpdateUI(null);
    }

    private void UpdateUI(string? property)
    {
        if (ViewModel is null) return;

        if (property is null or nameof(FinderViewModel.BarHeight))
            Height = ViewModel.BarHeight;

        CpuWidget.Visibility = ViewModel.ShowCpu ? Visibility.Visible : Visibility.Collapsed;
        MemoryWidget.Visibility = ViewModel.ShowMemory ? Visibility.Visible : Visibility.Collapsed;
        BatteryWidget.Visibility = ViewModel.ShowBattery ? Visibility.Visible : Visibility.Collapsed;
        NetworkWidget.Visibility = ViewModel.ShowNetwork ? Visibility.Visible : Visibility.Collapsed;
        AudioWidget.Visibility = ViewModel.ShowAudio ? Visibility.Visible : Visibility.Collapsed;

        CpuText.Text = $"CPU {ViewModel.CpuUsage:F0}%";
        MemoryText.Text = $"RAM {ViewModel.MemoryUsage:F0}%";
        BatteryText.Text = $"{ViewModel.BatteryPercent}%";
        ClockText.Text = ViewModel.ClockText;
        DateText.Text = ViewModel.DateText;

        TrayIcons.ItemsSource = ViewModel.TrayIcons;
    }
}
