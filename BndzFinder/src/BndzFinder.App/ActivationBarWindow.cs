using BndzFinder.Core.Settings;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Graphics;
using Windows.UI;
using WinRT.Interop;

namespace BndzFinder.App;

/// <summary>
/// MyDockFinder-style edge activation indicator at the screen border.
/// </summary>
public sealed class ActivationBarWindow : Window
{
    private readonly Rectangle _bar = new();

    public ActivationBarWindow()
    {
        Title = "Bndz-Finder Activation Bar";
        var root = new Grid();
        root.Children.Add(_bar);
        Content = root;

        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.SetBorderAndTitleBar(false, false);
            presenter.IsResizable = false;
            presenter.IsAlwaysOnTop = true;
        }

        AppWindow.IsShownInSwitchers = false;
    }

    public void Update(BndzFinderSettings settings, bool pointerNearEdge, int screenWidth, int screenHeight)
    {
        if (!settings.ShowDockActivationBar || !pointerNearEdge)
        {
            AppWindow.Hide();
            return;
        }

        var weight = Math.Max(2, settings.ActivationBarWeight);
        var length = Math.Max(48, settings.ActivationBarHeight * 40);
        var offset = Math.Max(0, settings.ActivationBarOffset);
        _bar.Fill = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255));

        var (left, top, width, height) = settings.DockPosition switch
        {
            Core.Models.DockPosition.Left => (offset, (screenHeight - length) / 2, weight, length),
            Core.Models.DockPosition.Right => (screenWidth - weight - offset, (screenHeight - length) / 2, weight, length),
            Core.Models.DockPosition.Top => ((screenWidth - length) / 2, offset, length, weight),
            _ => ((screenWidth - length) / 2, screenHeight - weight - offset, length, weight)
        };

        AppWindow.MoveAndResize(new RectInt32(left, top, width, height));
        _bar.Width = width;
        _bar.Height = height;
        AppWindow.Show();
    }
}
