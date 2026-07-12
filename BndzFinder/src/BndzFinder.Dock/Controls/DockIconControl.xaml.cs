using BndzFinder.Dock.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI;

namespace BndzFinder.Dock.Controls;

public sealed partial class DockIconControl : UserControl
{
    public static readonly DependencyProperty IconViewModelProperty =
        DependencyProperty.Register(
            nameof(IconViewModel),
            typeof(DockIconViewModel),
            typeof(DockIconControl),
            new PropertyMetadata(null, OnIconViewModelChanged));

    public DockIconViewModel? IconViewModel
    {
        get => (DockIconViewModel?)GetValue(IconViewModelProperty);
        set => SetValue(IconViewModelProperty, value);
    }

    public DockIconControl()
    {
        InitializeComponent();
    }

    private static void OnIconViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockIconControl control && e.NewValue is DockIconViewModel vm)
        {
            control.ApplyViewModel(vm);
        }
    }

    private void ApplyViewModel(DockIconViewModel vm)
    {
        var layout = vm.Layout;
        var size = layout.Size;

        Width = size + 8;
        Height = size + 20;

        IconBorder.Width = size;
        IconBorder.Height = size;
        IconBorder.CornerRadius = new CornerRadius(size * 0.22);

        IconScale.ScaleX = layout.Scale;
        IconScale.ScaleY = layout.Scale;
        IconLift.Y = layout.Y;
        IconTilt.Angle = layout.ImmersionTilt;

        ShadowLayer.Width = size * 0.85;
        ShadowLayer.Height = size * 0.2;
        ShadowLayer.Opacity = layout.ShadowOpacity;
        ShadowLayer.CornerRadius = new CornerRadius(size * 0.15);

        LightOverlay.Opacity = layout.LightIntensity;
        SelectRing.Opacity = layout.SelectGlow;
        ReflectionLayer.Opacity = layout.ReflectionOpacity;
        ReflectionLayer.Width = size * 0.9;

        RunningDot.Opacity = vm.ShowRunningDot ? 0.9 : 0;
        RunningDot.Fill = new SolidColorBrush(Color.FromArgb(230, 255, 255, 255));

        LabelText.Text = vm.DisplayName;
        HoverLabel.Opacity = vm.LabelOpacity;

        if (File.Exists(vm.IconCachePath))
        {
            IconImage.Source = new BitmapImage(new Uri(vm.IconCachePath));
        }

        if (layout.IsSeparator)
        {
            IconBorder.Visibility = Visibility.Collapsed;
            Width = 2;
            Height = size;
        }
    }
}
