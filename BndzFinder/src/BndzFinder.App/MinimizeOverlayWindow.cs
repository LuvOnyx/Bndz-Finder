using BndzFinder.Animations;
using BndzFinder.Core.Models;
using BndzFinder.Core.Services;
using Windows.UI;
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
    private readonly SkewTransform _skew = new();
    private readonly TranslateTransform _translate = new();
    private readonly MinimizeFrameCalculator _calculator = new();
    private readonly DispatcherTimer _timer = new();
    private MinimizeStartedInfo? _active;
    private MinimizeEffect _effect = MinimizeEffect.Genie;
    private int _frame;
    private int _totalFrames = 28;

    public MinimizeOverlayWindow()
    {
        Title = "Bndz-Finder Minimize";
        var root = new Grid { Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)) };
        _image.RenderTransform = new TransformGroup { Children = { _scale, _skew, _translate } };
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
        _effect = Enum.TryParse<MinimizeEffect>(info.Effect, true, out var parsed)
            ? parsed
            : MinimizeEffect.Genie;

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
        var t = _frame / (double)_totalFrames;

        var request = new MinimizeAnimationRequest
        {
            SourceWindow = (nint)_active.Handle,
            WindowSnapshot = [],
            TargetX = _active.TargetX - _active.X,
            TargetY = _active.TargetY - _active.Y
        };

        var state = _calculator.Calculate(request, _effect, t);

        _scale.ScaleX = Math.Max(0.05, state.ScaleX);
        _scale.ScaleY = Math.Max(0.05, state.ScaleY);
        _translate.X = state.TranslateX;
        _translate.Y = state.TranslateY;

        if (_effect == MinimizeEffect.Genie)
        {
            _skew.AngleX = state.GenieNeck * 28;
            _skew.CenterX = _active.Width * 0.5;
            _skew.CenterY = _active.Height;
        }
        else if (_effect == MinimizeEffect.Suck)
        {
            _skew.AngleX = state.Funnel * -18;
            _skew.CenterX = _active.Width * 0.5;
            _skew.CenterY = _active.Height * 0.5;
        }
        else
        {
            _skew.AngleX = 0;
        }

        if (_frame >= _totalFrames)
        {
            _timer.Stop();
            AppWindow.Hide();
            _active = null;
            _skew.AngleX = 0;
        }
    }
}
