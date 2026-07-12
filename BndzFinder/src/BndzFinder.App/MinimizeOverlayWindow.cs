using BndzFinder.Core.Services;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics;

namespace BndzFinder.App;

/// <summary>
/// Visible minimize-to-dock animation overlay (Genie/Scale/Suck).
/// </summary>
public sealed class MinimizeOverlayWindow : Window
{
    private readonly Image _image = new() { Stretch = Stretch.Fill };
    private readonly ScaleTransform _scale = new() { ScaleX = 1, ScaleY = 1 };
    private readonly TranslateTransform _translate = new();
    private readonly DispatcherTimer _timer = new();
    private MinimizeStartedInfo? _active;
    private int _frame;
    private const int TotalFrames = 24;

    public MinimizeOverlayWindow()
    {
        Title = "Bndz-Finder Minimize";
        var root = new Grid { Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)) };
        _image.RenderTransform = new TransformGroup { Children = { _scale, _translate } };
        root.Children.Add(_image);
        Content = root;

        if (AppWindow.Presenter is OverlappedPresenter p)
        {
            p.SetBorderAndTitleBar(false, false);
            p.IsResizable = false;
            p.IsAlwaysOnTop = true;
        }

        AppWindow.IsShownInSwitchers = false;
        _timer.Interval = TimeSpan.FromMilliseconds(16);
        _timer.Tick += (_, _) => AdvanceFrame();
    }

    public void Play(MinimizeStartedInfo info)
    {
        _active = info;
        _frame = 0;

        if (!string.IsNullOrWhiteSpace(info.SnapshotBase64))
        {
            try
            {
                var bytes = Convert.FromBase64String(info.SnapshotBase64);
                var temp = Path.Combine(Path.GetTempPath(), "BndzFinder", $"min-{info.Handle}.png");
                Directory.CreateDirectory(Path.GetDirectoryName(temp)!);
                File.WriteAllBytes(temp, bytes);
                _image.Source = new BitmapImage(new Uri(temp));
            }
            catch
            {
                _image.Source = null;
            }
        }

        AppWindow.MoveAndResize(new RectInt32(info.X, info.Y, Math.Max(64, info.Width), Math.Max(64, info.Height)));
        AppWindow.Show();
        Activate();
        _timer.Start();
    }

    private void AdvanceFrame()
    {
        if (_active is null) return;
        _frame++;
        var t = _frame / (double)TotalFrames;
        var eased = t < 0.5 ? 4 * t * t * t : 1 - Math.Pow(-2 * t + 2, 3) / 2;

        _scale.ScaleX = _scale.ScaleY = 1.0 - eased * 0.92;
        _translate.X = eased * 120;
        _translate.Y = eased * (_active.Height * 0.6);

        if (_frame >= TotalFrames)
        {
            _timer.Stop();
            AppWindow.Hide();
            _active = null;
        }
    }
}
