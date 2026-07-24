using BndzFinder.Core.Design;
using BndzFinder.Finder.ViewModels;
using BndzFinder.Interop;
using BndzFinder.Shell.Assets;
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
        ApplyFigmaChrome();
    }

    private void ApplyFigmaChrome()
    {
        try
        {
            var figma = new FigmaAssetService();
            var mark = figma.ResolveAppleMark();
            if (mark is not null && File.Exists(mark))
            {
                AppleMarkImage.Source = new BitmapImage(new Uri(mark));
                AppleMarkImage.Visibility = Visibility.Visible;
                AppleMarkPath.Visibility = Visibility.Collapsed;
            }

            var tokens = figma.LoadTokens();
            if (tokens?.MenuBar is { } menu && menu.Height > 0)
                Height = menu.Height;
        }
        catch
        {
            // Keep XAML vector fallback.
        }
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FinderBarControl control) control.Bind();
    }

    private void Bind()
    {
        if (ViewModel is null) return;
        ViewModel.PropertyChanged += (_, args) => UpdateUI(args.PropertyName);
        Height = Math.Max(AppleDesignMetrics.MenuBarHeight, ViewModel.BarHeight);
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
        var thumbSize = Math.Max(40, Math.Min(StageManagerViewModel.ThumbnailSize, 72));
        foreach (var window in StageManagerViewModel.Windows)
        {
            var frame = new Border
            {
                Width = thumbSize,
                Height = Math.Max(16, AppleDesignMetrics.MenuBarHeight - 6),
                CornerRadius = new CornerRadius(4),
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
            Height = Math.Max(AppleDesignMetrics.MenuBarHeight, ViewModel.BarHeight);

        if (property is null or nameof(FinderViewModel.ActiveAppName))
            AppTitle.Text = string.IsNullOrWhiteSpace(ViewModel.ActiveAppName) ? "Finder" : ViewModel.ActiveAppName;

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

        CpuText.Text = $"{ViewModel.CpuUsage:F0}%";
        GpuText.Text = $"{ViewModel.GpuUsage:F0}%";
        MemoryText.Text = $"{ViewModel.MemoryUsage:F0}%";
        DiskText.Text = $"{ViewModel.DiskUsage:F0}%";
        BatteryText.Text = $"{ViewModel.BatteryPercent}%";
        WeatherText.Text = ViewModel.WeatherText;
        KeyboardText.Text = ViewModel.KeyboardLayout;
        ClockText.Text = ViewModel.ClockText;
        DateText.Text = ViewModel.DateText;
        ApplyTimeSkin(ViewModel.TimeSkinImagePath);
        RenderTrayIcons();
    }

    private void RenderTrayIcons()
    {
        if (ViewModel is null) return;
        TrayIcons.Items.Clear();
        foreach (var icon in ViewModel.TrayIcons)
        {
            var content = new Grid { Width = 18, Height = 18 };
            if (icon.IconData is { Length: > 0 })
            {
                try
                {
                    var temp = Path.Combine(Path.GetTempPath(), "BndzFinder", $"tray-{icon.IconId}.png");
                    Directory.CreateDirectory(Path.GetDirectoryName(temp)!);
                    File.WriteAllBytes(temp, icon.IconData);
                    content.Children.Add(new Image
                    {
                        Width = 14,
                        Height = 14,
                        Source = new BitmapImage(new Uri(temp)),
                        Stretch = Stretch.Uniform
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
                    Text = string.IsNullOrWhiteSpace(icon.Tooltip) ? "•" : icon.Tooltip[..Math.Min(1, icon.Tooltip.Length)],
                    FontSize = 10
                });
            }

            var button = new Button
            {
                Style = (Style)Resources["StatusItem"],
                Content = content,
                Tag = icon,
                Padding = new Thickness(3, 0, 3, 0)
            };
            ToolTipService.SetToolTip(button, icon.Tooltip);
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

    private void ShowMenu(FrameworkElement anchor, params (string Label, Action? Action)[] items)
    {
        var flyout = new MenuFlyout { Placement = FlyoutPlacementMode.Bottom };
        foreach (var (label, action) in items)
        {
            if (label == "-")
            {
                flyout.Items.Add(new MenuFlyoutSeparator());
                continue;
            }

            var item = new MenuFlyoutItem { Text = label };
            if (action is not null)
                item.Click += (_, _) => action();
            flyout.Items.Add(item);
        }
        flyout.ShowAt(anchor);
    }

    private void OnAppleMenuClick(object sender, RoutedEventArgs e) =>
        ShowMenu(AppleMenuButton,
            ("About This PC", () => ViewModel?.OpenSystemInfo()),
            ("-", null),
            ("System Settings…", () => ViewModel?.OpenPreferences()),
            ("-", null),
            ("Force Quit…", () => ViewModel?.OpenTaskManager()),
            ("-", null),
            ("Sleep", () => ViewModel?.SleepDisplay()),
            ("Restart…", null),
            ("Shut Down…", null),
            ("-", null),
            ("Lock Screen", () => ViewModel?.LockWorkstation()));

    private void OnAppMenuClick(object sender, RoutedEventArgs e) =>
        ShowMenu(AppTitleButton,
            ($"About {AppTitle.Text}", null),
            ("-", null),
            ("Preferences…", () => ViewModel?.OpenPreferences()),
            ("-", null),
            ("Hide", null),
            ("Hide Others", null),
            ("Show All", null),
            ("-", null),
            ("Quit", null));

    private void OnFileMenuClick(object sender, RoutedEventArgs e) =>
        ShowMenu(FileMenuButton,
            ("New Finder Window", () => ViewModel?.OpenExplorer()),
            ("New Folder", null),
            ("-", null),
            ("Close Window", null),
            ("-", null),
            ("Get Info", null));

    private void OnEditMenuClick(object sender, RoutedEventArgs e) =>
        ShowMenu(EditMenuButton,
            ("Undo", null),
            ("Redo", null),
            ("-", null),
            ("Cut", null),
            ("Copy", null),
            ("Paste", null),
            ("Select All", null));

    private void OnViewMenuClick(object sender, RoutedEventArgs e) =>
        ShowMenu(ViewMenuButton,
            ("Show Dock", () => ViewModel?.ToggleDock()),
            ("Show Launchpad", () => ViewModel?.ShowLaunchpad()),
            ("Show Stage Manager", () => ViewModel?.ShowStageManager()),
            ("-", null),
            ("Enter Full Screen", null));

    private void OnWindowMenuClick(object sender, RoutedEventArgs e) =>
        ShowMenu(WindowMenuButton,
            ("Minimize", null),
            ("Zoom", null),
            ("-", null),
            ("Bring All to Front", null));

    private void OnHelpMenuClick(object sender, RoutedEventArgs e) =>
        ShowMenu(HelpMenuButton,
            ("BNDZ Finder Help", null),
            ("-", null),
            ("About BNDZ Finder", () => ViewModel?.OpenPreferences()));

    private void OnAudioClick(object sender, RoutedEventArgs e) => ShowControlCenter("audio", AudioWidget);
    private void OnBluetoothClick(object sender, RoutedEventArgs e) => ShowControlCenter("bluetooth", BluetoothWidget);
    private void OnDisplayClick(object sender, RoutedEventArgs e) => ShowControlCenter("display", DisplayWidget);
    private void OnNetworkClick(object sender, RoutedEventArgs e) => ShowControlCenter("wifi", NetworkWidget);
    private void OnControlCenterClick(object sender, RoutedEventArgs e) => ShowControlCenter("control", ControlCenterWidget);
    private void OnSearchClick(object sender, RoutedEventArgs e) => ViewModel?.OpenSearch();
    private void OnNotificationsClick(object sender, RoutedEventArgs e) => ViewModel?.OpenActionCenter();
    private void OnCpuClick(object sender, RoutedEventArgs e) => ShowControlCenter("cpu", CpuWidget);
    private void OnCalendarClick(object sender, RoutedEventArgs e) => ShowControlCenter("calendar", ClockWidget);

    private void ApplyTimeSkin(string? skinPath)
    {
        if (!string.IsNullOrWhiteSpace(skinPath) && File.Exists(skinPath))
        {
            ClockWidget.Background = new ImageBrush
            {
                ImageSource = new BitmapImage(new Uri(skinPath)),
                Stretch = Stretch.UniformToFill,
                Opacity = 0.18
            };
            return;
        }

        ClockWidget.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
    }
}
