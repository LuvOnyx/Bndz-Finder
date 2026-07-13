using BndzFinder.Launchpad.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BndzFinder.Launchpad.Controls;

public sealed class LaunchpadAppItem
{
    public required AppCatalogEntry Entry { get; init; }
    public required LaunchpadViewModel ViewModel { get; init; }
    public Uri? IconUri => Entry.IconUri;
    public string Name => Entry.Name;
    public int IconSize => ViewModel.IconSize;
    public int CellSize => ViewModel.CellSize;
    public Visibility LabelVisibility => ViewModel.HideLabels ? Visibility.Collapsed : Visibility.Visible;
}

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
            if (ViewModel is not null && e.ClickedItem is LaunchpadAppItem item)
                ViewModel.LaunchCommand.Execute(item.Entry);
        };
    }

    private void Bind()
    {
        if (ViewModel is null) return;
        ViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(LaunchpadViewModel.FilteredApps))
                RefreshGrid();
            else if (args.PropertyName is nameof(LaunchpadViewModel.DisplayPage)
                     or nameof(LaunchpadViewModel.TotalPages)
                     or nameof(LaunchpadViewModel.FilteredCount)
                     or nameof(LaunchpadViewModel.IconSize)
                     or nameof(LaunchpadViewModel.HideLabels))
                UpdatePagination();
        };
        RefreshGrid();
        UpdatePagination();
    }

    private void RefreshGrid()
    {
        if (ViewModel is null) return;
        AppGrid.ItemsSource = ViewModel.FilteredApps
            .Select(a => new LaunchpadAppItem { Entry = a, ViewModel = ViewModel })
            .ToList();
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
