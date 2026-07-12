using BndzFinder.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BndzFinder.Finder.ViewModels;

public partial class FinderViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly SystemMetricsService _metrics;

    [ObservableProperty] private double _cpuUsage;
    [ObservableProperty] private double _memoryUsage;
    [ObservableProperty] private double _gpuUsage;
    [ObservableProperty] private int _batteryPercent = 100;
    [ObservableProperty] private string _clockText = DateTime.Now.ToString("h:mm tt");
    [ObservableProperty] private IReadOnlyList<TrayProxyItem> _trayIcons = [];

    public FinderViewModel(ISettingsService settings, SystemMetricsService? metrics = null)
    {
        _settings = settings;
        _metrics = metrics ?? new SystemMetricsService();
        _ = StartPollingAsync();
    }

    private async Task StartPollingAsync()
    {
        while (true)
        {
            CpuUsage = await _metrics.GetCpuUsageAsync().ConfigureAwait(false);
            MemoryUsage = await _metrics.GetMemoryUsageAsync().ConfigureAwait(false);
            GpuUsage = await _metrics.GetGpuUsageAsync().ConfigureAwait(false);
            BatteryPercent = await _metrics.GetBatteryPercentAsync().ConfigureAwait(false);
            ClockText = DateTime.Now.ToString($"{_settings.Current.TimeFormat}");
            await Task.Delay(1000).ConfigureAwait(false);
        }
    }
}

public sealed class TrayProxyItem
{
    public required string Tooltip { get; init; }
    public byte[]? IconData { get; init; }
}

public sealed class SystemMetricsService
{
    public Task<double> GetCpuUsageAsync() => Task.FromResult(0.0);
    public Task<double> GetMemoryUsageAsync() => Task.FromResult(0.0);
    public Task<double> GetGpuUsageAsync() => Task.FromResult(0.0);
    public Task<int> GetBatteryPercentAsync() => Task.FromResult(100);
}
