using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BndzFinder.Finder.Controls;

public sealed partial class ControlCenterFlyout : UserControl
{
    public static readonly DependencyProperty PanelProperty =
        DependencyProperty.Register(nameof(Panel), typeof(string), typeof(ControlCenterFlyout),
            new PropertyMetadata("audio", (d, e) => ((ControlCenterFlyout)d).ShowPanel(e.NewValue?.ToString())));

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
        ControlPanel.Visibility = Visibility.Collapsed;
        WifiPanel.Visibility = Visibility.Collapsed;
        BluetoothPanel.Visibility = Visibility.Collapsed;
        AudioPanel.Visibility = Visibility.Collapsed;
        DisplayPanel.Visibility = Visibility.Collapsed;
        MetricsPanel.Visibility = Visibility.Collapsed;
        CalendarPanel.Visibility = Visibility.Collapsed;

        switch (panel?.ToLowerInvariant())
        {
            case "control":
                PanelTitle.Text = "Control Center";
                ControlPanel.Visibility = Visibility.Visible;
                break;
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
            case "cpu":
            case "metrics":
                PanelTitle.Text = "System";
                MetricsPanel.Visibility = Visibility.Visible;
                MetricsBody.Text = "Live CPU, GPU, memory, and disk usage appear in the menu bar when enabled.";
                break;
            case "calendar":
                PanelTitle.Text = "Calendar";
                CalendarPanel.Visibility = Visibility.Visible;
                CalendarBody.Text = DateTime.Now.ToString("dddd, MMMM d, yyyy");
                break;
            default:
                PanelTitle.Text = "Sound";
                AudioPanel.Visibility = Visibility.Visible;
                break;
        }
    }
}
