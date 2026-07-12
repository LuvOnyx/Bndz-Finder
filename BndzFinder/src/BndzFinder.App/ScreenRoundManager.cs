using BndzFinder.Core.Services;
using Windows.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Graphics;
using WinRT.Interop;

namespace BndzFinder.App;

/// <summary>
/// MyDockFinder-style rounded screen corners — opaque masks at each monitor corner.
/// </summary>
public sealed class ScreenRoundManager : IDisposable
{
    private readonly List<ScreenRoundCornerWindow> _windows = [];

    public void Apply(IReadOnlyList<DisplayMonitorInfo> monitors, bool enabled, int radius, string colorHex)
    {
        DisposeWindows();
        if (!enabled || radius <= 0 || !OperatingSystem.IsWindows()) return;

        var fill = ParseColor(colorHex);
        foreach (var monitor in monitors)
        {
            _windows.Add(CreateCorner(monitor, ScreenRoundCorner.TopLeft, radius, fill));
            _windows.Add(CreateCorner(monitor, ScreenRoundCorner.TopRight, radius, fill));
            _windows.Add(CreateCorner(monitor, ScreenRoundCorner.BottomLeft, radius, fill));
            _windows.Add(CreateCorner(monitor, ScreenRoundCorner.BottomRight, radius, fill));
        }
    }

    public void Dispose() => DisposeWindows();

    private void DisposeWindows()
    {
        foreach (var w in _windows)
            w.Close();
        _windows.Clear();
    }

    private static ScreenRoundCornerWindow CreateCorner(
        DisplayMonitorInfo monitor, ScreenRoundCorner corner, int radius, Color fill)
    {
        var (left, top) = corner switch
        {
            ScreenRoundCorner.TopLeft => (monitor.Left, monitor.Top),
            ScreenRoundCorner.TopRight => (monitor.Left + monitor.Width - radius, monitor.Top),
            ScreenRoundCorner.BottomLeft => (monitor.Left, monitor.Top + monitor.Height - radius),
            _ => (monitor.Left + monitor.Width - radius, monitor.Top + monitor.Height - radius)
        };

        var window = new ScreenRoundCornerWindow(corner, radius, fill);
        window.AppWindow.MoveAndResize(new RectInt32(left, top, radius, radius));
        window.Activate();
        return window;
    }

    private static Color ParseColor(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length < 6) return Color.FromArgb(255, 0, 0, 0);
        return Color.FromArgb(
            255,
            Convert.ToByte(hex[..2], 16),
            Convert.ToByte(hex[2..4], 16),
            Convert.ToByte(hex[4..6], 16));
    }
}

internal enum ScreenRoundCorner { TopLeft, TopRight, BottomLeft, BottomRight }

internal sealed class ScreenRoundCornerWindow : Window
{
    public ScreenRoundCornerWindow(ScreenRoundCorner corner, int radius, Color fill)
    {
        Title = "Bndz-Finder Screen Round";
        var root = new Canvas { Width = radius, Height = radius, Background = new SolidColorBrush(fill) };

        var geometry = new GeometryGroup { FillRule = FillRule.EvenOdd };
        geometry.Children.Add(new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, radius, radius) });

        var (cx, cy) = corner switch
        {
            ScreenRoundCorner.TopLeft => (radius, radius),
            ScreenRoundCorner.TopRight => (0, radius),
            ScreenRoundCorner.BottomLeft => (radius, 0),
            _ => (0.0, 0.0)
        };
        geometry.Children.Add(new EllipseGeometry
        {
            Center = new Windows.Foundation.Point(cx, cy),
            RadiusX = radius,
            RadiusY = radius
        });

        root.Children.Add(new Path
        {
            Fill = new SolidColorBrush(fill),
            Data = geometry
        });

        Content = root;

        if (AppWindow.Presenter is OverlappedPresenter p)
        {
            p.SetBorderAndTitleBar(false, false);
            p.IsResizable = false;
            p.IsAlwaysOnTop = true;
        }

        AppWindow.IsShownInSwitchers = false;
        var hwnd = WindowNative.GetWindowHandle(this);
        if (OperatingSystem.IsWindows())
            _ = SetWindowLong(hwnd, -20, GetWindowLong(hwnd, -20) | 0x80000 | 0x20); // WS_EX_LAYERED | WS_EX_TRANSPARENT
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern int GetWindowLong(nint hWnd, int nIndex);

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern int SetWindowLong(nint hWnd, int nIndex, int dwNewLong);
}
