using BndzFinder.Core.Models;
using BndzFinder.Dock.Helpers;
using BndzFinder.Dock.ViewModels;
using BndzFinder.Theming.Glass;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI;

namespace BndzFinder.Dock.Controls;

public sealed partial class DockBarControl : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(DockViewModel),
            typeof(DockBarControl),
            new PropertyMetadata(null, OnViewModelChanged));

    private readonly List<DockIconControl> _iconControls = [];
    private Flyout? _folderFlyout;
    private Flyout? _previewFlyout;
    private DwmPreviewHost? _dwmPreviewHost;
    private int? _dragSourceIndex;
    private double _dragStartX;

    public DockViewModel? ViewModel
    {
        get => (DockViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public DockBarControl()
    {
        InitializeComponent();
        AllowDrop = true;
        DragOver += OnDragOver;
        Drop += OnDrop;
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockBarControl control)
        {
            control.BindViewModel();
        }
    }

    private void BindViewModel()
    {
        if (ViewModel is null) return;
        ViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(DockViewModel.IconViewModels)
                or nameof(DockViewModel.Appearance)
                or nameof(DockViewModel.DockBarWidth)
                or nameof(DockViewModel.DockBarHeight))
            {
                RefreshDock();
            }
            else if (args.PropertyName is nameof(DockViewModel.ActiveFolderStackPath)
                     && !string.IsNullOrWhiteSpace(ViewModel.ActiveFolderStackPath))
            {
                ShowFolderStack(ViewModel.ActiveFolderStackPath);
            }
        };
        ViewModel.PreviewShowRequested += OnPreviewShowRequested;
        ViewModel.PreviewHideRequested += OnPreviewHideRequested;
        RefreshDock();
    }

    private void OnPreviewShowRequested(long hwnd)
    {
        if (ViewModel is null || !ViewModel.PreviewEnabled) return;

        _previewFlyout ??= new Flyout { Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Top };
        _previewFlyout.Closed += (_, _) => _dwmPreviewHost?.HidePreview();

        if (ViewModel.WindowPreviewService is { } previewService && OperatingSystem.IsWindows())
        {
            _dwmPreviewHost ??= new DwmPreviewHost();
            _dwmPreviewHost.Configure(previewService, ViewModel.PreviewSize, (int)(ViewModel.PreviewSize * 0.62));
            _previewFlyout.Content = _dwmPreviewHost;
            _previewFlyout.ShowAt(IconCanvas);
            _dwmPreviewHost.ShowPreview(hwnd);
            return;
        }

        var capture = ViewModel.CaptureService?.CaptureWindow((nint)hwnd);
        if (capture is null) return;

        var image = new Image
        {
            Width = Math.Min(320, capture.Width),
            Height = Math.Min(200, capture.Height),
            Stretch = Stretch.Uniform
        };
        var bitmap = BgraBitmapHelper.CreateFromBgra(capture.Pixels, capture.Width, capture.Height, 320);
        if (bitmap is not null) image.Source = bitmap;
        _previewFlyout.Content = image;
        _previewFlyout.ShowAt(IconCanvas);
    }

    private void OnPreviewHideRequested()
    {
        _dwmPreviewHost?.HidePreview();
        _previewFlyout?.Hide();
    }

    private void RefreshDock()
    {
        if (ViewModel?.Appearance is null) return;

        ApplyGlass(ViewModel.Appearance.Glass);
        ApplyDockSkin(ViewModel.Appearance.DockSkinImagePath);
        GlassBackdrop.Width = ViewModel.DockBarWidth;
        GlassBackdrop.Height = ViewModel.DockBarHeight;
        DockShadow.Width = ViewModel.DockBarWidth;
        DockShadow.Height = ViewModel.DockBarHeight;

        IconCanvas.Children.Clear();
        _iconControls.Clear();

        foreach (var iconVm in ViewModel.IconViewModels)
        {
            var control = new DockIconControl
            {
                IconViewModel = iconVm
            };
            Canvas.SetLeft(control, iconVm.RenderX);
            Canvas.SetTop(control, iconVm.RenderY);
            control.PointerEntered += (_, _) =>
            {
                if (iconVm.Layout?.Index is int index)
                    ViewModel.OnIconPointerEntered(index);
            };
            control.PointerExited += (_, _) => ViewModel.OnIconPointerExited();
            control.PointerPressed += (_, e) =>
            {
                if (iconVm.Layout?.Index is int index)
                {
                    _dragSourceIndex = index;
                    _dragStartX = e.GetCurrentPoint(IconCanvas).Position.X;
                }
            };
            control.PointerReleased += (_, e) =>
            {
                if (ViewModel is null || _dragSourceIndex is not int from) return;
                var delta = e.GetCurrentPoint(IconCanvas).Position.X - _dragStartX;
                if (Math.Abs(delta) < 24) return;
                var to = Math.Clamp(delta > 0 ? from + 1 : from - 1, 0, ViewModel.IconViewModels.Count - 1);
                _ = ViewModel.ReorderItemAsync(from, to);
                _dragSourceIndex = null;
            };
            control.Tapped += (_, _) =>
            {
                if (iconVm.Layout?.Item is { } item)
                    _ = ViewModel.HandleItemClickAsync(item);
            };
            control.RightTapped += (_, e) =>
            {
                if (iconVm.Layout?.Item is { } item)
                    ShowIconContextMenu(item, control, e);
            };
            IconCanvas.Children.Add(control);
            _iconControls.Add(control);
        }

        IconCanvas.Width = ViewModel.DockBarWidth - 48;
        IconCanvas.Height = ViewModel.DockBarHeight;
    }

    public (double X, double Y)? GetIconScreenCenter(string itemId)
    {
        foreach (var control in _iconControls)
        {
            if (control.IconViewModel?.Layout?.Item?.Id != itemId) continue;
            if (control.ActualWidth <= 0 || control.ActualHeight <= 0) continue;

            var transform = control.TransformToVisual(null);
            if (transform is null) continue;

            var center = transform.TransformPoint(new Windows.Foundation.Point(
                control.ActualWidth / 2,
                control.ActualHeight / 2));
            return (center.X, center.Y);
        }

        return null;
    }

    private void ShowFolderStack(string folderPath)
    {
        _folderFlyout ??= new Flyout { Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Top };
        var options = ViewModel?.ActiveFolderStackOptions;
        _folderFlyout.Content = new FolderStackFlyout
        {
            ViewModel = new FolderStackViewModel(
                folderPath,
                options?.View ?? Core.Models.FolderStackView.Automatic,
                options?.Sort ?? Core.Models.FolderSortMode.Name)
        };
        _folderFlyout.ShowAt(IconCanvas);
    }

    private void ApplyDockSkin(string? skinPath)
    {
        if (!string.IsNullOrWhiteSpace(skinPath) && File.Exists(skinPath))
        {
            GlassFill.Background = new ImageBrush
            {
                ImageSource = new BitmapImage(new Uri(skinPath)),
                Stretch = Stretch.Fill,
                Opacity = 0.45
            };
            return;
        }

        if (GlassFill.Background is not AcrylicBrush)
        {
            DockAcrylic.TintColor = Color.FromArgb(255, 38, 38, 38);
            DockAcrylic.TintOpacity = 0.32;
            DockAcrylic.TintLuminosityOpacity = 0.92;
            DockAcrylic.FallbackColor = Color.FromArgb(204, 42, 42, 42);
            GlassFill.Background = DockAcrylic;
        }
    }

    private void ApplyGlass(GlassConfiguration glass)
    {
        GlassBackdrop.CornerRadius = new CornerRadius(glass.CornerRadius);
        GlassFill.CornerRadius = new CornerRadius(Math.Max(0, glass.CornerRadius - 1));
        GlassHighlight.CornerRadius = GlassFill.CornerRadius;
        GlassInnerEdge.CornerRadius = GlassFill.CornerRadius;
        DockShadow.CornerRadius = new CornerRadius(glass.CornerRadius + 2);
        DockShadow.Opacity = Math.Clamp(glass.ShadowOpacity, 0.2, 0.55);

        GlassBackdrop.BorderBrush = new SolidColorBrush(
            Color.FromArgb((byte)(Math.Clamp(glass.BorderOpacity, 0.12, 0.4) * 255), 255, 255, 255));
        GlassHighlight.Opacity = Math.Clamp(glass.HighlightOpacity, 0.15, 0.5);

        var tint = ParseColor(glass.TintColor, 1.0);
        DockAcrylic.TintColor = Color.FromArgb(255, tint.R, tint.G, tint.B);
        DockAcrylic.FallbackColor = Color.FromArgb(
            (byte)(Math.Clamp(glass.Opacity, 0.55, 0.9) * 255),
            tint.R, tint.G, tint.B);

        if (glass.Effect == GlassEffectKind.Translucent)
        {
            DockAcrylic.TintOpacity = glass.TintOpacity;
            DockAcrylic.TintLuminosityOpacity = 0.85;
            GlassBackdrop.Opacity = glass.Opacity;
        }
        else if (glass.Effect == GlassEffectKind.Mica)
        {
            DockAcrylic.TintOpacity = glass.TintOpacity;
            DockAcrylic.TintLuminosityOpacity = glass.Luminosity;
            GlassBackdrop.Opacity = glass.Opacity;
        }
        else if (glass.Effect == GlassEffectKind.LiquidGlass)
        {
            var liquid = glass.LiquidGlass ?? new LiquidGlassParameters();
            DockAcrylic.TintOpacity = Math.Clamp(0.22 + liquid.Distortion * 0.1, 0.18, 0.36);
            DockAcrylic.TintLuminosityOpacity = Math.Clamp(0.9 + liquid.EdgeHighlight * 0.08, 0.88, 0.98);
            GlassBackdrop.Opacity = Math.Clamp(glass.Opacity + liquid.Refraction * 0.05, 0.7, 0.92);
            GlassHighlight.Opacity = Math.Clamp(0.3 + liquid.EdgeHighlight * 0.2, 0.25, 0.55);
        }
        else
        {
            // Acrylic — Tahoe frosted default
            DockAcrylic.TintOpacity = glass.TintOpacity;
            DockAcrylic.TintLuminosityOpacity = glass.Luminosity;
            GlassBackdrop.Opacity = glass.Opacity;
        }

        GlassFill.Background = DockAcrylic;
    }

    private static Color ParseColor(string hex, double opacity)
    {
        hex = hex.TrimStart('#');
        if (hex.Length < 6) return Color.FromArgb((byte)(opacity * 255), 30, 30, 30);
        return Color.FromArgb(
            (byte)(opacity * 255),
            Convert.ToByte(hex[..2], 16),
            Convert.ToByte(hex[2..4], 16),
            Convert.ToByte(hex[4..6], 16));
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (ViewModel is null) return;
        var pos = e.GetCurrentPoint(IconCanvas).Position;
        ViewModel.OnPointerMoved(pos.X);
    }

    private void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        ViewModel?.OnPointerExited();
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (ViewModel is null || ViewModel.IsLockedForDrop)
        {
            e.AcceptedOperation = DataPackageOperation.None;
            return;
        }

        if (e.DataView.Contains(StandardDataFormats.StorageItems))
            e.AcceptedOperation = DataPackageOperation.Copy;
        else
            e.AcceptedOperation = DataPackageOperation.None;
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (ViewModel is null || ViewModel.IsLockedForDrop) return;
        if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;

        var items = await e.DataView.GetStorageItemsAsync();
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Path)) continue;
            await ViewModel.PinDroppedPathCommand.ExecuteAsync(item.Path);
        }
    }

    private void ShowIconContextMenu(DockItem item, DockIconControl anchor, RightTappedRoutedEventArgs e)
    {
        if (ViewModel is null) return;
        e.Handled = true;

        var menu = new MenuFlyout();
        var isEphemeral = item.Id.StartsWith("running:", StringComparison.OrdinalIgnoreCase);
        var isApp = item.Kind is Core.Models.DockItemKind.Application or Core.Models.DockItemKind.File;

        if (isApp)
        {
            menu.Items.Add(new MenuFlyoutItem
            {
                Text = "Open",
                Command = ViewModel.LaunchItemCommand,
                CommandParameter = item
            });
            menu.Items.Add(new MenuFlyoutItem
            {
                Text = "Show in Explorer",
                Command = ViewModel.ShowInExplorerCommand,
                CommandParameter = item
            });
        }

        if (item.Kind is Core.Models.DockItemKind.Folder)
        {
            menu.Items.Add(new MenuFlyoutItem
            {
                Text = "Show in Explorer",
                Command = ViewModel.ShowInExplorerCommand,
                CommandParameter = item
            });
        }

        if (isApp && isEphemeral && !item.IsPinned)
        {
            menu.Items.Add(new MenuFlyoutItem
            {
                Text = "Keep in Dock",
                Command = ViewModel.PinRunningItemCommand,
                CommandParameter = item
            });
        }

        if (item.Kind is Core.Models.DockItemKind.Folder)
        {
            menu.Items.Add(new MenuFlyoutItem
            {
                Text = "Open Stack",
                Command = ViewModel.HandleItemClickCommand,
                CommandParameter = item
            });
            var sortMenu = new MenuFlyoutSubItem { Text = "Sort By" };
            sortMenu.Items.Add(new MenuFlyoutItem { Text = "Name" });
            sortMenu.Items.Add(new MenuFlyoutItem { Text = "Date Modified" });
            menu.Items.Add(sortMenu);
        }

        if (item.Kind is Core.Models.DockItemKind.SystemPreferences)
        {
            menu.Items.Add(new MenuFlyoutItem
            {
                Text = "Dock Preferences…",
                Command = ViewModel.HandleItemClickCommand,
                CommandParameter = item
            });
        }

        if (isApp && _runningAppsContains(item))
        {
            menu.Items.Add(new MenuFlyoutSeparator());
            menu.Items.Add(new MenuFlyoutItem
            {
                Text = "Quit",
                Command = ViewModel.QuitApplicationCommand,
                CommandParameter = item
            });
        }

        if (!isEphemeral && item.IsPinned && item.Kind is Core.Models.DockItemKind.Application or Core.Models.DockItemKind.File or Core.Models.DockItemKind.Folder)
        {
            menu.Items.Add(new MenuFlyoutSeparator());
            menu.Items.Add(new MenuFlyoutItem
            {
                Text = "Remove from Dock",
                Command = ViewModel.RemoveFromDockCommand,
                CommandParameter = item
            });
        }

        if (menu.Items.Count > 0)
            menu.ShowAt(anchor);
    }

    private bool _runningAppsContains(DockItem item) => ViewModel?.IsRunning(item) == true;
}
