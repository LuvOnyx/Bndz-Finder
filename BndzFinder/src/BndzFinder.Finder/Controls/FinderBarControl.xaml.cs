using BndzFinder.Finder.ViewModels;
using BndzFinder.Interop;
using BndzFinder.StageManager.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI;

namespace BndzFinder.Finder.Controls;

public sealed partial class FinderBarControl : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(FinderViewModel), typeof(FinderBarControl),
            new PropertyMetadata(null, OnViewModelChanged));

    public static readonly DependencyProperty StageManagerViewModelProperty =
        DependencyProperty.Register(nameof(StageManagerViewModel), typeof(StageManagerViewModel), typeof(FinderBarControl),
            new PropertyMetadata(null, (d, _) => ((FinderBarControl)d).BindStageManager()));

    private Flyout? _controlCenterFlyout;

    public FinderViewModel? ViewModel
    {
        get => (FinderViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public StageManagerViewModel? StageManagerViewModel
    {
        get => (StageManagerViewModel?)GetValue(StageManagerViewModelProperty);
        set => SetValue(StageManagerViewModelProperty, value);
    }

    public Visibility StageStripVisibility =>
        ViewModel?.ShowStageManagerInFinder == true && StageManagerViewModel is not null
            ? Visibility.Visible
            : Visibility.Collapsed;

    public FinderBarControl()
    {
        InitializeComponent();
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
        BindStageManager();
    }

    private void BindStageManager()
    {
        if (StageManagerViewModel is null) return;
        StageManagerViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(StageManagerViewModel.Windows))
                RenderStageStrip();
        };
        RenderStageStrip();
    }

    private void RenderStageStrip()
    {
        if (StageManagerViewModel is null) return;
        StageStrip.Children.Clear();
        var thumbSize = Math.Max(48, Math.Min(StageManagerViewModel.ThumbnailSize, 96));
        foreach (var window in StageManagerViewModel.Windows)
        {
            var frame = new Border
            {
                Width = thumbSize,
                Height = thumbSize * 0.62,
                CornerRadius = new CornerRadius(6),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Background = new SolidColorBrush(Color.FromArgb(60, 30, 30, 30))
            };

            if (window.Thumbnail is { Length: > 0 })
            {
                try
                {
                    var temp = Path.Combine(Path.GetTempPath(), "BndzFinder", $"stage-{window.Hwnd}.png");
                    Directory.CreateDirectory(Path.GetDirectoryName(temp)!);
                    File.WriteAllBytes(temp, window.Thumbnail);
                    frame.Child = new Image
                    {
                        Source = new BitmapImage(new Uri(temp)),
                        Stretch = Stretch.UniformToFill
                    };
                }
                catch { }
            }

            var button = new Button
            {
                Padding = new Thickness(0),
                Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                BorderThickness = new Thickness(0),
                Content = frame,
                Tag = window
            };
            button.Click += (_, _) =>
            {
                if (button.Tag is WindowThumbnailItem item)
                    StageManagerViewModel.FocusWindowCommand.Execute(item);
            };
            StageStrip.Children.Add(button);
        }
    }

    private void UpdateUI(string? property)
    {
        if (ViewModel is null) return;

        if (property is null or nameof(FinderViewModel.BarHeight))
            Height = ViewModel.BarHeight;

        CpuWidget.Visibility = ViewModel.ShowCpu ? Visibility.Visible : Visibility.Collapsed;
        GpuWidget.Visibility = ViewModel.ShowGpu ? Visibility.Visible : Visibility.Collapsed;
        MemoryWidget.Visibility = ViewModel.ShowMemory ? Visibility.Visible : Visibility.Collapsed;
        DiskWidget.Visibility = ViewModel.ShowDisk ? Visibility.Visible : Visibility.Collapsed;
        BatteryWidget.Visibility = ViewModel.ShowBattery ? Visibility.Visible : Visibility.Collapsed;
        NetworkWidget.Visibility = ViewModel.ShowNetwork ? Visibility.Visible : Visibility.Collapsed;
        WeatherWidget.Visibility = ViewModel.ShowWeather ? Visibility.Visible : Visibility.Collapsed;
        AudioWidget.Visibility = ViewModel.ShowAudio ? Visibility.Visible : Visibility.Collapsed;
        BluetoothWidget.Visibility = ViewModel.ShowBluetooth ? Visibility.Visible : Visibility.Collapsed;
        DisplayWidget.Visibility = ViewModel.ShowDisplay ? Visibility.Visible : Visibility.Collapsed;
        KeyboardWidget.Visibility = ViewModel.ShowKeyboard ? Visibility.Visible : Visibility.Collapsed;
        MediaWidget.Visibility = ViewModel.ShowMediaControl ? Visibility.Visible : Visibility.Collapsed;
        NotificationsWidget.Visibility = ViewModel.ShowNotifications ? Visibility.Visible : Visibility.Collapsed;

        CpuText.Text = $"CPU {ViewModel.CpuUsage:F0}%";
        GpuText.Text = $"GPU {ViewModel.GpuUsage:F0}%";
        MemoryText.Text = $"RAM {ViewModel.MemoryUsage:F0}%";
        DiskText.Text = $"Disk {ViewModel.DiskUsage:F0}%";
        BatteryText.Text = $"{ViewModel.BatteryPercent}%";
        WeatherText.Text = ViewModel.WeatherText;
        KeyboardText.Text = ViewModel.KeyboardLayout;
        ClockText.Text = ViewModel.ClockText;
        DateText.Text = ViewModel.DateText;
        ApplyTimeSkin(ViewModel.TimeSkinImagePath);
        TrayIcons.Items.Clear();
        foreach (var icon in ViewModel.TrayIcons)
        {
            var content = new Grid { Width = 20, Height = 20 };
            if (icon.IconData is { Length: > 0 })
            {
                try
                {
                    var temp = Path.Combine(Path.GetTempPath(), "BndzFinder", $"tray-{icon.IconId}.png");
                    Directory.CreateDirectory(Path.GetDirectoryName(temp)!);
                    File.WriteAllBytes(temp, icon.IconData);
                    content.Children.Add(new Image
                    {
                        Width = 16,
                        Height = 16,
                        Source = new BitmapImage(new Uri(temp))
                    });
                }
                catch
                {
                    content.Children.Add(new TextBlock { Text = "•", FontSize = 10 });
                }
            }
            else
            {
                content.Children.Add(new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(icon.Tooltip) ? "•" : icon.Tooltip[..Math.Min(2, icon.Tooltip.Length)],
                    FontSize = 9
                });
            }

            var button = new Button
            {
                Style = (Style)Resources["FinderWidgetButton"],
                Content = content,
                Tag = icon
            };
            button.Click += (_, _) =>
            {
                if (button.Tag is TrayProxyItem item)
                    ViewModel?.ClickTrayIconCommand.Execute(item);
            };
            TrayIcons.Items.Add(button);
        }
    }

    private void ShowControlCenter(string panel, FrameworkElement anchor)
    {
        _controlCenterFlyout ??= new Flyout { Placement = FlyoutPlacementMode.Bottom };
        _controlCenterFlyout.Content = new ControlCenterFlyout { Panel = panel };
        _controlCenterFlyout.ShowAt(anchor);
    }

    private void OnAudioClick(object sender, RoutedEventArgs e) => ShowControlCenter("audio", AudioWidget);
    private void OnBluetoothClick(object sender, RoutedEventArgs e) => ShowControlCenter("bluetooth", BluetoothWidget);
    private void OnDisplayClick(object sender, RoutedEventArgs e) => ShowControlCenter("display", DisplayWidget);
    private void OnCpuClick(object sender, RoutedEventArgs e) => ViewModel?.OpenControlCenter("cpu");
    private void OnCalendarClick(object sender, RoutedEventArgs e) => ViewModel?.OpenControlCenter("calendar");

    private void OnPreferencesClick(object sender, RoutedEventArgs e) => ViewModel?.OpenPreferences();

    private void ApplyTimeSkin(string? skinPath)
    {
        if (!string.IsNullOrWhiteSpace(skinPath) && File.Exists(skinPath))
        {
            ClockWidget.Background = new ImageBrush
            {
                ImageSource = new BitmapImage(new Uri(skinPath)),
                Stretch = Stretch.UniformToFill,
                Opacity = 0.22
            };
            return;
        }

        ClockWidget.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
    }
}
