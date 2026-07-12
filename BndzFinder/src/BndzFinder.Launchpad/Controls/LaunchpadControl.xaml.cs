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
            else if (args.PropertyName is nameof(LaunchpadViewModel.DisplayPage)
                     or nameof(LaunchpadViewModel.TotalPages)
                     or nameof(LaunchpadViewModel.FilteredCount))
                UpdatePagination();
        };
        AppGrid.ItemsSource = ViewModel.FilteredApps;
        UpdatePagination();
    }

    private void UpdatePagination()
    {
        if (ViewModel is null) return;
        PageIndicator.Text = $"{ViewModel.DisplayPage} / {ViewModel.TotalPages}";
        PrevPageButton.IsEnabled = ViewModel.CurrentPage > 0;
        NextPageButton.IsEnabled = (ViewModel.CurrentPage + 1) * ViewModel.PageSize < ViewModel.FilteredCount;
    }

    private void OnPreviousPage(object sender, RoutedEventArgs e) =>
        ViewModel?.PreviousPageCommand.Execute(null);

    private void OnNextPage(object sender, RoutedEventArgs e) =>
        ViewModel?.NextPageCommand.Execute(null);
}
