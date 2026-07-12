using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BndzFinder.Finder.Controls;

public sealed partial class ControlCenterFlyout : UserControl
{
    public static readonly DependencyProperty PanelProperty =
        DependencyProperty.Register(nameof(Panel), typeof(string), typeof(ControlCenterFlyout),
            new PropertyMetadata("audio", (_, e) => ((ControlCenterFlyout)_).ShowPanel(e.NewValue?.ToString())));

    public string Panel
    {
        get => (string)GetValue(PanelProperty);
        set => SetValue(PanelProperty, value);
    }

    public ControlCenterFlyout()
    {
        InitializeComponent();
        ShowPanel(Panel);
    }

    private void ShowPanel(string? panel)
    {
        WifiPanel.Visibility = Visibility.Collapsed;
        BluetoothPanel.Visibility = Visibility.Collapsed;
        AudioPanel.Visibility = Visibility.Collapsed;
        DisplayPanel.Visibility = Visibility.Collapsed;

        switch (panel?.ToLowerInvariant())
        {
            case "wifi":
                PanelTitle.Text = "Wi-Fi";
                WifiPanel.Visibility = Visibility.Visible;
                break;
            case "bluetooth":
                PanelTitle.Text = "Bluetooth";
                BluetoothPanel.Visibility = Visibility.Visible;
                break;
            case "display":
                PanelTitle.Text = "Display";
                DisplayPanel.Visibility = Visibility.Visible;
                break;
            default:
                PanelTitle.Text = "Sound";
                AudioPanel.Visibility = Visibility.Visible;
                break;
        }
    }
}
