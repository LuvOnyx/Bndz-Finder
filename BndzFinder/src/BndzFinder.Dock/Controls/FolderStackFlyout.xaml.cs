using BndzFinder.Core.Models;
using BndzFinder.Dock.ViewModels;
using BndzFinder.Shell.Dock;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace BndzFinder.Dock.Controls;

public sealed partial class FolderStackFlyout : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(FolderStackViewModel), typeof(FolderStackFlyout),
            new PropertyMetadata(null, (_, e) =>
            {
                if (_.Equals(e.NewValue)) return;
                var flyout = (FolderStackFlyout)_;
                flyout.Render();
                if (e.NewValue is FolderStackViewModel vm)
                    vm.PropertyChanged += (_, args) =>
                    {
                        if (args.PropertyName is nameof(FolderStackViewModel.Entries))
                            flyout.Render();
                    };
            }));

    public FolderStackViewModel? ViewModel
    {
        get => (FolderStackViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public FolderStackFlyout() => InitializeComponent();

    private void Render()
    {
        if (ViewModel is null) return;
        FanCanvas.Visibility = Visibility.Collapsed;
        GridPanel.Visibility = Visibility.Collapsed;
        ListPanel.Visibility = Visibility.Collapsed;
        GridPanel.Items.Clear();
        ListPanel.Children.Clear();
        FanCanvas.Children.Clear();

        var view = FolderStackLayoutHelper.ResolveView(ViewModel.ViewMode, ViewModel.Entries.Count);

        switch (view)
        {
            case FolderStackView.Fan:
                RenderFan();
                break;
            case FolderStackView.Grid:
                GridPanel.Visibility = Visibility.Visible;
                foreach (var entry in ViewModel.Entries)
                    GridPanel.Items.Add(CreateEntryButton(entry, 40));
                break;
            default:
                ListPanel.Visibility = Visibility.Visible;
                foreach (var entry in ViewModel.Entries)
                    ListPanel.Children.Add(CreateEntryButton(entry, 24, horizontal: true));
                break;
        }
    }

    private void RenderFan()
    {
        FanCanvas.Visibility = Visibility.Visible;
        var originX = 40.0;
        var originY = 120.0;

        foreach (var entry in ViewModel!.Entries)
        {
            var radians = entry.FanAngle * Math.PI / 180.0;
            var x = originX + Math.Sin(radians) * entry.FanRadius;
            var y = originY - Math.Cos(radians) * entry.FanRadius;
            var btn = CreateEntryButton(entry, 36);
            Canvas.SetLeft(btn, x);
            Canvas.SetTop(btn, y);
            FanCanvas.Children.Add(btn);
        }
    }

    private Button CreateEntryButton(FolderStackEntry entry, int iconSize, bool horizontal = false)
    {
        var image = new Image
        {
            Width = iconSize,
            Height = iconSize,
            Stretch = Stretch.Uniform
        };
        if (File.Exists(entry.IconCachePath))
            image.Source = new BitmapImage(new Uri(entry.IconCachePath));

        UIElement content = horizontal
            ? new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children = { image, new TextBlock { Text = entry.Name, VerticalAlignment = VerticalAlignment.Center } }
            }
            : new StackPanel
            {
                Spacing = 4,
                Width = 64,
                Children =
                {
                    image,
                    new TextBlock
                    {
                        Text = entry.Name,
                        FontSize = 10,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        TextAlignment = TextAlignment.Center
                    }
                }
            };

        var btn = new Button
        {
            Content = content,
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0)),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(4),
            Tag = entry
        };
        btn.Click += (_, _) => _ = ViewModel?.OpenEntryCommand.ExecuteAsync(entry);
        return btn;
    }
}
