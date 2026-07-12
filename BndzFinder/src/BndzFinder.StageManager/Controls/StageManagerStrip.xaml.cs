using BndzFinder.StageManager.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
            if (args.PropertyName is nameof(StageManagerViewModel.Windows))
                WindowThumbnails.ItemsSource = ViewModel.Windows;
        };
        WindowThumbnails.ItemsSource = ViewModel.Windows;
    }
}
