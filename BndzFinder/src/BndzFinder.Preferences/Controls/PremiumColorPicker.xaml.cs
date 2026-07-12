using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace BndzFinder.Preferences.Controls;

public sealed partial class PremiumColorPicker : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(PremiumColorPicker),
            new PropertyMetadata("Accent color", (_, e) => ((PremiumColorPicker)_).PickerLabel.Text = e.NewValue?.ToString()));

    public static readonly DependencyProperty SelectedHexProperty =
        DependencyProperty.Register(nameof(SelectedHex), typeof(string), typeof(PremiumColorPicker),
            new PropertyMetadata("#0078D4", OnSelectedHexChanged));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string SelectedHex
    {
        get => (string)GetValue(SelectedHexProperty);
        set => SetValue(SelectedHexProperty, value);
    }

    public event EventHandler<string>? ColorChanged;

    private bool _updating;

    public PremiumColorPicker()
    {
        InitializeComponent();
        PickerLabel.Text = Label;
        HueSlider.ValueChanged += (_, _) => UpdateFromSliders();
        SaturationSlider.ValueChanged += (_, _) => UpdateFromSliders();
        ValueSlider.ValueChanged += (_, _) => UpdateFromSliders();
        HexBox.TextChanged += (_, _) => UpdateFromHex();
        ApplyHex(SelectedHex);
    }

    private static void OnSelectedHexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PremiumColorPicker picker && e.NewValue is string hex)
            picker.ApplyHex(hex);
    }

    private void ApplyHex(string hex)
    {
        if (_updating) return;
        _updating = true;
        var color = PremiumColor.FromHex(hex);
        var (h, s, v) = PremiumColorMath.ToHsv(color);
        HueSlider.Value = h;
        SaturationSlider.Value = s * 100;
        ValueSlider.Value = v * 100;
        HexBox.Text = color.ToHex();
        SwatchPreview.Background = new SolidColorBrush(Color.FromArgb(255, color.R, color.G, color.B));
        _updating = false;
    }

    private void UpdateFromSliders()
    {
        if (_updating) return;
        var color = PremiumColor.FromHsv(HueSlider.Value, SaturationSlider.Value / 100, ValueSlider.Value / 100);
        var hex = color.ToHex();
        HexBox.Text = hex;
        SwatchPreview.Background = new SolidColorBrush(Color.FromArgb(255, color.R, color.G, color.B));
        SelectedHex = hex;
        ColorChanged?.Invoke(this, hex);
    }

    private void UpdateFromHex()
    {
        if (_updating || string.IsNullOrWhiteSpace(HexBox.Text)) return;
        ApplyHex(HexBox.Text);
        SelectedHex = HexBox.Text;
        ColorChanged?.Invoke(this, HexBox.Text);
    }
}
