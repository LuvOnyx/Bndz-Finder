using BndzFinder.Core.Models;
using BndzFinder.Dock.ViewModels;
using BndzFinder.Shell.Dock;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace BndzFinder.Dock.Controls;

public sealed partial class FolderStackFlyout : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(FolderStackViewModel), typeof(FolderStackFlyout),
            new PropertyMetadata(null, (_, e) =>
            {
                if (_.Equals(e.NewValue)) return;
                ((FolderStackFlyout)_).Render();
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
        GridList.Visibility = Visibility.Collapsed;
        ListView.Visibility = Visibility.Collapsed;

        var view = FolderStackLayoutHelper.ResolveView(ViewModel.ViewMode, ViewModel.Entries.Count);

        switch (view)
        {
            case FolderStackView.Fan:
                RenderFan();
                break;
            case FolderStackView.Grid:
                GridList.Visibility = Visibility.Visible;
                GridList.ItemsSource = ViewModel.Entries;
                break;
            default:
                ListView.Visibility = Visibility.Visible;
                ListView.ItemsSource = ViewModel.Entries;
                break;
        }
    }

    private void RenderFan()
    {
        FanCanvas.Visibility = Visibility.Visible;
        FanCanvas.Children.Clear();
        var originX = 40.0;
        var originY = 120.0;

        foreach (var entry in ViewModel!.Entries)
        {
            var radians = entry.FanAngle * Math.PI / 180.0;
            var x = originX + Math.Sin(radians) * entry.FanRadius;
            var y = originY - Math.Cos(radians) * entry.FanRadius;

            var btn = new Button
            {
                Content = new StackPanel
                {
                    Spacing = 2,
                    Children =
                    {
                        new FontIcon { Glyph = "\uE8B7", FontSize = 24 },
                        new TextBlock { Text = entry.Name, FontSize = 9, MaxWidth = 64, TextTrimming = TextTrimming.CharacterEllipsis }
                    }
                },
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0)),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(4)
            };
            btn.Click += (_, _) => _ = ViewModel.OpenEntryAsync(entry);
            Canvas.SetLeft(btn, x);
            Canvas.SetTop(btn, y);
            FanCanvas.Children.Add(btn);
        }
    }
}
