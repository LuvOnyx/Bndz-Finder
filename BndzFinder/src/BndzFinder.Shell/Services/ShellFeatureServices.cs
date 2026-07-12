using BndzFinder.Shell.Badges;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace BndzFinder.Shell.Services;

public sealed class WeatherService
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(8) };
    private string _condition = "Clear";
    private double _celsius = 22;
    private DateTimeOffset _lastFetch = DateTimeOffset.MinValue;

    public string GetCurrentCondition() => _condition;
    public double GetCurrentCelsius() => _celsius;
    public IReadOnlyList<string> GetForecast() => [_condition, "Partly Cloudy", "Rain"];

    public async Task RefreshAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        if (DateTimeOffset.UtcNow - _lastFetch < TimeSpan.FromMinutes(15))
            return;

        try
        {
            var url =
                $"https://api.open-meteo.com/v1/forecast?latitude={latitude:F4}&longitude={longitude:F4}&current_weather=true";
            var payload = await Client.GetFromJsonAsync<OpenMeteoResponse>(url, cancellationToken).ConfigureAwait(false);
            if (payload?.CurrentWeather is null)
                return;

            _celsius = payload.CurrentWeather.Temperature;
            _condition = MapWeatherCode(payload.CurrentWeather.WeatherCode);
            _lastFetch = DateTimeOffset.UtcNow;
        }
        catch
        {
            // Keep last known values on transient network failures.
        }
    }

    private static string MapWeatherCode(int code) => code switch
    {
        0 => "Clear",
        1 or 2 => "Partly Cloudy",
        3 => "Cloudy",
        45 or 48 => "Fog",
        51 or 53 or 55 => "Drizzle",
        61 or 63 or 65 => "Rain",
        71 or 73 or 75 => "Snow",
        80 or 81 or 82 => "Showers",
        95 or 96 or 99 => "Thunderstorm",
        _ => "Clear"
    };

    private sealed class OpenMeteoResponse
    {
        [JsonPropertyName("current_weather")]
        public OpenMeteoCurrentWeather? CurrentWeather { get; init; }
    }

    private sealed class OpenMeteoCurrentWeather
    {
        [JsonPropertyName("temperature")]
        public double Temperature { get; init; }

        [JsonPropertyName("weathercode")]
        public int WeatherCode { get; init; }
    }
}

public sealed class ProgressBarMirrorService
{
    private readonly Dictionary<string, double> _progress = new(StringComparer.OrdinalIgnoreCase);

    public void SetProgress(string appPath, double value) =>
        _progress[appPath] = Math.Clamp(value, 0, 1);

    public void ClearProgress(string appPath) => _progress.Remove(appPath);

    public double GetProgress(string appPath) =>
        _progress.TryGetValue(appPath, out var value) ? value : 0;
}

public sealed class BadgePollingService : IDisposable
{
    private readonly BadgeAdapterRegistry _registry;
    private readonly Dictionary<string, int?> _counts = new(StringComparer.OrdinalIgnoreCase);
    private CancellationTokenSource? _cts;

    public BadgePollingService(BadgeAdapterRegistry? registry = null) =>
        _registry = registry ?? new BadgeAdapterRegistry();

    public event EventHandler<BadgeCountsUpdatedEventArgs>? CountsUpdated;

    public IReadOnlyDictionary<string, int?> GetCounts() => _counts;

    public void Start(TimeSpan interval)
    {
        Stop();
        _cts = new CancellationTokenSource();
        _ = PollAsync(interval, _cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts = null;
    }

    private async Task PollAsync(TimeSpan interval, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            foreach (var adapter in _registry.Adapters)
            {
                var count = await adapter.GetUnreadCountAsync(ct).ConfigureAwait(false);
                _counts[adapter.AppId] = count;
            }
            CountsUpdated?.Invoke(this, new BadgeCountsUpdatedEventArgs(_counts));
            await Task.Delay(interval, ct).ConfigureAwait(false);
        }
    }

    public void Dispose() => Stop();
}

public sealed class BadgeCountsUpdatedEventArgs : EventArgs
{
    public BadgeCountsUpdatedEventArgs(IReadOnlyDictionary<string, int?> counts) => Counts = counts;
    public IReadOnlyDictionary<string, int?> Counts { get; }
}

public static class WindowTitleBadgeParser
{
    private static readonly Regex CountPattern = new(@"\((\d+)\)|\[(\d+)\]", RegexOptions.Compiled);

    public static int? ParseUnreadCount(IEnumerable<string> titles)
    {
        var max = 0;
        var found = false;
        foreach (var title in titles)
        {
            if (string.IsNullOrWhiteSpace(title)) continue;
            foreach (Match match in CountPattern.Matches(title))
            {
                var value = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
                if (int.TryParse(value, out var count) && count > max)
                {
                    max = count;
                    found = true;
                }
            }
        }

        return found ? max : null;
    }
}
