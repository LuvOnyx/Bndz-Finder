using BndzFinder.Core.Services;
using BndzFinder.Shell.Services;
using BndzFinder.Interop;
using BndzFinder.Theming;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BndzFinder.Finder.ViewModels;

public partial class FinderViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly ISystemMetricsService _metrics;
    private readonly ITrayMirrorFacade _trayMirror;
    private readonly WeatherService _weather;
    private readonly IThemePackResolver? _themePacks;
    private bool _traySyncedFromIpc;

    [ObservableProperty] private double _cpuUsage;
    [ObservableProperty] private double _memoryUsage;
    [ObservableProperty] private double _gpuUsage;
    [ObservableProperty] private double _diskUsage;
    [ObservableProperty] private double _networkUpKbps;
    [ObservableProperty] private int _batteryPercent = 100;
    [ObservableProperty] private string _batteryTimeRemaining = string.Empty;
    [ObservableProperty] private string _clockText = DateTime.Now.ToString("h:mm tt");
    [ObservableProperty] private string _dateText = DateTime.Now.ToString("ddd MMM d");
    [ObservableProperty] private string _weatherText = "—";
    [ObservableProperty] private string _keyboardLayout = "EN";
    [ObservableProperty] private IReadOnlyList<TrayProxyItem> _trayIcons = [];
    [ObservableProperty] private bool _isDark;
    [ObservableProperty] private double _barHeight = 28;
    [ObservableProperty] private string? _timeSkinImagePath;

    public bool ShowCpu => _settings.Current.ShowCpu;
    public bool ShowGpu => _settings.Current.ShowGpu;
    public bool ShowMemory => _settings.Current.ShowMemory;
    public bool ShowDisk => _settings.Current.ShowDisk;
    public bool ShowNetwork => _settings.Current.ShowNetwork;
    public bool ShowBattery => _settings.Current.ShowBattery;
    public bool ShowWeather => _settings.Current.ShowWeather;
    public bool ShowAudio => _settings.Current.ShowAudio;
    public bool ShowBluetooth => _settings.Current.ShowBluetooth;
    public bool ShowDisplay => _settings.Current.ShowDisplay;
    public bool ShowNotifications => _settings.Current.ShowNotifications;
    public bool ShowKeyboard => _settings.Current.ShowKeyboard;
    public bool ShowMediaControl => _settings.Current.ShowMediaControl;
    public bool ShowMicrophone => _settings.Current.ShowMicrophone;
    public bool ShowLyrics => _settings.Current.ShowLyrics;
    public bool ShowStageManagerInFinder => _settings.Current.ShowStageManagerInFinder;

    public FinderViewModel(
        ISettingsService settings,
        ISystemMetricsService? metrics = null,
        ITrayMirrorFacade? trayMirror = null,
        WeatherService? weather = null,
        IThemePackResolver? themePacks = null)
    {
        _settings = settings;
        _metrics = metrics ?? new WindowsSystemMetricsService();
        _trayMirror = trayMirror ?? new TrayMirrorFacade(new TrayIconMirrorService());
        _weather = weather ?? new WeatherService();
        _themePacks = themePacks;
        BarHeight = settings.Current.FinderHeight;
        TimeSkinImagePath = _themePacks?.ResolveTimeSkinPath(settings.Current);
        _settings.SettingsChanged += (_, _) =>
        {
            BarHeight = _settings.Current.FinderHeight;
            TimeSkinImagePath = _themePacks?.ResolveTimeSkinPath(_settings.Current);
            NotifyWidgetVisibility();
        };
        _ = StartPollingAsync();
    }

    [RelayCommand]
    public void OpenControlCenter(string panel) => ControlCenterRequested?.Invoke(panel);

    public event Action<string>? ControlCenterRequested;
    public event Action? PreferencesRequested;

    public void OpenPreferences() => PreferencesRequested?.Invoke();

    public void NotifyWidgetVisibility()
    {
        OnPropertyChanged(nameof(ShowCpu));
        OnPropertyChanged(nameof(ShowGpu));
        OnPropertyChanged(nameof(ShowMemory));
        OnPropertyChanged(nameof(ShowDisk));
        OnPropertyChanged(nameof(ShowNetwork));
        OnPropertyChanged(nameof(ShowBattery));
        OnPropertyChanged(nameof(ShowWeather));
        OnPropertyChanged(nameof(ShowAudio));
        OnPropertyChanged(nameof(ShowBluetooth));
        OnPropertyChanged(nameof(ShowDisplay));
        OnPropertyChanged(nameof(ShowKeyboard));
        OnPropertyChanged(nameof(ShowMediaControl));
        OnPropertyChanged(nameof(ShowNotifications));
        OnPropertyChanged(nameof(ShowMicrophone));
        OnPropertyChanged(nameof(ShowLyrics));
        OnPropertyChanged(nameof(ShowStageManagerInFinder));
    }

    [RelayCommand]
    public void ClickTrayIcon(TrayProxyItem item) => _trayMirror.ForwardClick(item);

    public void ApplyTrayIconsFromPayload(string json)
    {
        _traySyncedFromIpc = true;
        TrayIcons = ShellIpcParsers.ParseTrayIcons(json)
            .Select(s => new TrayProxyItem
            {
                Tooltip = s.Tooltip,
                IconId = s.IconId,
                OwnerWindow = (nint)s.OwnerWindow,
                IconData = string.IsNullOrWhiteSpace(s.IconDataBase64)
                    ? null
                    : Convert.FromBase64String(s.IconDataBase64)
            })
            .ToList();
    }

    private async Task StartPollingAsync()
    {
        while (true)
        {
            try
            {
                CpuUsage = await _metrics.GetCpuUsageAsync().ConfigureAwait(false);
                MemoryUsage = await _metrics.GetMemoryUsageAsync().ConfigureAwait(false);
                GpuUsage = await _metrics.GetGpuUsageAsync().ConfigureAwait(false);
                DiskUsage = await _metrics.GetDiskUsageAsync().ConfigureAwait(false);
                NetworkUpKbps = await _metrics.GetNetworkKbpsAsync().ConfigureAwait(false);
                BatteryPercent = await _metrics.GetBatteryPercentAsync().ConfigureAwait(false);
                BatteryTimeRemaining = await _metrics.GetBatteryTimeRemainingAsync().ConfigureAwait(false);
                ClockText = DateTime.Now.ToString(_settings.Current.TimeFormat);
                DateText = DateTime.Now.ToString(_settings.Current.DateFormat);
                if (_settings.Current.ShowWeather)
                {
                    await _weather.RefreshAsync(
                        _settings.Current.WeatherLatitude,
                        _settings.Current.WeatherLongitude).ConfigureAwait(false);
                    WeatherText = $"{_weather.GetCurrentCondition()} {_weather.GetCurrentCelsius():0}°";
                }
                else WeatherText = "—";
                if (!_traySyncedFromIpc)
                    TrayIcons = await _trayMirror.GetIconsAsync().ConfigureAwait(false);
            }
            catch
            {
                // Polling continues on transient WMI failures.
            }
            await Task.Delay(1000).ConfigureAwait(false);
        }
    }
}

public sealed class TrayProxyItem
{
    public string Tooltip { get; init; } = string.Empty;
    public nint OwnerWindow { get; init; }
    public uint IconId { get; init; }
    public byte[]? IconData { get; init; }
}

public interface ITrayMirrorFacade
{
    Task<IReadOnlyList<TrayProxyItem>> GetIconsAsync();
    void ForwardClick(TrayProxyItem item);
}

public sealed class TrayMirrorFacade : ITrayMirrorFacade
{
    private readonly TrayIconMirrorService _mirror;

    public TrayMirrorFacade(TrayIconMirrorService mirror) => _mirror = mirror;

    public Task<IReadOnlyList<TrayProxyItem>> GetIconsAsync()
    {
        var icons = _mirror.GetVisibleTrayIcons();
        return Task.FromResult<IReadOnlyList<TrayProxyItem>>(icons.Select(i => new TrayProxyItem
        {
            Tooltip = i.Tooltip,
            OwnerWindow = i.OwnerWindow,
            IconId = i.IconId,
            IconData = i.IconData
        }).ToList());
    }

    public void ForwardClick(TrayProxyItem item) => _mirror.ForwardClick(item.OwnerWindow, item.IconId);
}
