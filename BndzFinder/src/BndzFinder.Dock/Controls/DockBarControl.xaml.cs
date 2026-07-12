using BndzFinder.Dock.ViewModels;
using BndzFinder.Theming.Glass;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
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

    public DockViewModel? ViewModel
    {
        get => (DockViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public DockBarControl()
    {
        InitializeComponent();
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
        };
        RefreshDock();
    }

    private void RefreshDock()
    {
        if (ViewModel?.Appearance is null) return;

        ApplyGlass(ViewModel.Appearance.Glass);
        GlassBackdrop.Width = ViewModel.DockBarWidth;
        GlassBackdrop.Height = ViewModel.DockBarHeight;

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
            control.PointerEntered += (_, _) => ViewModel.OnIconPointerEnteredCommand.Execute(iconVm.Layout.Index);
            control.Tapped += (_, _) => ViewModel.HandleItemClickCommand.Execute(iconVm.Layout.Item);
            IconCanvas.Children.Add(control);
            _iconControls.Add(control);
        }

        IconCanvas.Width = ViewModel.DockBarWidth - 48;
        IconCanvas.Height = ViewModel.DockBarHeight;
    }

    private void ApplyGlass(GlassConfiguration glass)
    {
        GlassBackdrop.CornerRadius = new CornerRadius(glass.CornerRadius);
        GlassBackdrop.Opacity = glass.Opacity;
        GlassBackdrop.BorderBrush = new SolidColorBrush(
            Color.FromArgb((byte)(glass.BorderOpacity * 255), 255, 255, 255));

        if (glass.Effect == GlassEffectKind.Translucent)
        {
            DockAcrylic.TintOpacity = glass.TintOpacity;
            DockAcrylic.FallbackColor = ParseColor(glass.TintColor, glass.Opacity);
        }
        else if (glass.Effect == GlassEffectKind.Mica)
        {
            DockAcrylic.TintColor = ParseColor(glass.TintColor, 1.0);
            DockAcrylic.TintOpacity = glass.TintOpacity;
        }
        else
        {
            DockAcrylic.TintOpacity = glass.TintOpacity;
            DockAcrylic.TintLuminosityOpacity = glass.Luminosity;
        }
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
        ViewModel.OnPointerMovedCommand.Execute(pos.X);
    }

    private void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        ViewModel?.OnPointerExitedCommand.Execute(null);
    }
}
