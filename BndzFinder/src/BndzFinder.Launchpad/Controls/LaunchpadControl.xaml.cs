using BndzFinder.Launchpad.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BndzFinder.Launchpad.Controls;

public sealed partial class LaunchpadControl : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(LaunchpadViewModel), typeof(LaunchpadControl),
            new PropertyMetadata(null, (_, __) => ((LaunchpadControl)_).Bind()));

    public LaunchpadViewModel? ViewModel
    {
        get => (LaunchpadViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public LaunchpadControl()
    {
        InitializeComponent();
        SearchBox.TextChanged += (_, _) =>
        {
            if (ViewModel is not null) ViewModel.SearchQuery = SearchBox.Text;
        };
        AppGrid.ItemClick += (_, e) =>
        {
            if (ViewModel is not null && e.ClickedItem is AppCatalogEntry entry)
                ViewModel.LaunchCommand.Execute(entry);
        };
    }

    private void Bind()
    {
        if (ViewModel is null) return;
        ViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(LaunchpadViewModel.FilteredApps))
                AppGrid.ItemsSource = ViewModel.FilteredApps;
        };
        AppGrid.ItemsSource = ViewModel.FilteredApps;
    }
}
