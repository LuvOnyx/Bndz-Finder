using BndzFinder.Shell.Badges;

namespace BndzFinder.Shell.Services;

public sealed class WeatherService
{
    public string GetCurrentCondition() => "Clear";
    public double GetCurrentCelsius() => 22.0;
    public IReadOnlyList<string> GetForecast() => ["Clear", "Partly Cloudy", "Rain"];
}

public sealed class ProgressBarMirrorService
{
    private readonly Dictionary<string, double> _progress = new(StringComparer.OrdinalIgnoreCase);

    public void SetProgress(string appPath, double value) =>
        _progress[appPath] = Math.Clamp(value, 0, 1);

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
