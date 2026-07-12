using BndzFinder.Interop;
using BndzFinder.StageManager.Helpers;
using BndzFinder.StageManager.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace BndzFinder.StageManager.Controls;

public sealed partial class StageManagerStrip : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(StageManagerViewModel), typeof(StageManagerStrip),
            new PropertyMetadata(null, (_, __) => ((StageManagerStrip)_).Bind()));

    public StageManagerViewModel? ViewModel
    {
        get => (StageManagerViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public StageManagerStrip() => InitializeComponent();

    private void Bind()
    {
        if (ViewModel is null) return;
        ViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(StageManagerViewModel.Windows)
                or nameof(StageManagerViewModel.ThumbnailSize)
                or nameof(StageManagerViewModel.ShowTitles))
            {
                RenderWindows();
            }
        };
        RenderWindows();
    }

    private void RenderWindows()
    {
        if (ViewModel is null) return;
        WindowThumbnails.Children.Clear();

        var size = Math.Clamp(ViewModel.ThumbnailSize, 80, 320);
        foreach (var window in ViewModel.Windows)
        {
            var root = new Grid { Width = size, Height = size * 0.62 };
            var thumbnail = ThumbnailBitmapHelper.CreateFromBgra(
                window.Thumbnail, window.ThumbnailWidth, window.ThumbnailHeight, size);
            if (thumbnail is not null)
            {
                root.Children.Add(new Image
                {
                    Source = thumbnail,
                    Stretch = Stretch.UniformToFill,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                });
            }
            else
            {
                root.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(120, 50, 50, 50)),
                    CornerRadius = new CornerRadius(6)
                });
            }

            if (ViewModel.ShowTitles)
            {
                root.Children.Add(new TextBlock
                {
                    Text = window.Title,
                    FontSize = 10,
                    Margin = new Thickness(6),
                    VerticalAlignment = VerticalAlignment.Bottom,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Foreground = new SolidColorBrush(Colors.White)
                });
            }

            var button = new Button
            {
                Content = root,
                Padding = new Thickness(0),
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromArgb(64, 40, 40, 40)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(48, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Tag = window
            };
            button.Click += (_, _) =>
            {
                if (button.Tag is WindowThumbnailItem item)
                    ViewModel.FocusWindowCommand.Execute(item);
            };
            button.RightTapped += (_, _) =>
            {
                if (button.Tag is WindowThumbnailItem item)
                    ViewModel.CloseWindowCommand.Execute(item);
            };

            WindowThumbnails.Children.Add(button);
        }
    }
}
