using BndzFinder.Core.Models;
using BndzFinder.Core.Services;
using BndzFinder.Shell.Dock;
using BndzFinder.Shell.Icons;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BndzFinder.Dock.ViewModels;

public partial class DockViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IDockLayoutEngine _layoutEngine;
    private readonly IIconPipeline _iconPipeline;

    [ObservableProperty] private IReadOnlyList<DockLayoutItem> _layoutItems = [];
    [ObservableProperty] private double _cursorPosition;
    [ObservableProperty] private bool _isVisible = true;

    public DockViewModel(
        ISettingsService settings,
        IDockLayoutEngine? layoutEngine = null,
        IIconPipeline? iconPipeline = null)
    {
        _settings = settings;
        _layoutEngine = layoutEngine ?? new FisheyeDockLayoutEngine();
        _iconPipeline = iconPipeline ?? new MacStyleIconPipeline();
        _settings.SettingsChanged += (_, _) => RefreshLayout();
        RefreshLayout();
    }

    [RelayCommand]
    public void OnPointerMoved(double position) => CursorPosition = position;

    [RelayCommand]
    public async Task PinItemAsync(string path)
    {
        var settings = _settings.Current;
        settings.DockItems.Add(new DockItem
        {
            Id = Guid.NewGuid().ToString("N"),
            Kind = DockItemKind.Application,
            TargetPath = path,
            DisplayName = Path.GetFileNameWithoutExtension(path),
            IsPinned = true,
            SortOrder = settings.DockItems.Count
        });
        await _iconPipeline.ProcessAsync(path).ConfigureAwait(false);
        await _settings.SaveAsync().ConfigureAwait(false);
        RefreshLayout();
    }

    [RelayCommand]
    public async Task LaunchItemAsync(DockItem item)
    {
        if (!OperatingSystem.IsWindows()) return;
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(item.TargetPath) { UseShellExecute = true });
        await Task.CompletedTask;
    }

    public void RefreshLayout()
    {
        var s = _settings.Current;
        LayoutItems = _layoutEngine.ComputeLayout(
            s.DockItems,
            CursorPosition,
            s.IconSize,
            s.IconMaxSize,
            s.IconSpace);
    }
}
