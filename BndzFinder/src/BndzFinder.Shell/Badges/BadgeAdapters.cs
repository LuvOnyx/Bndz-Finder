namespace BndzFinder.Shell.Badges;

public interface IBadgeAdapter
{
    string AppId { get; }
    Task<int?> GetUnreadCountAsync(CancellationToken cancellationToken = default);
}

public sealed class DiscordBadgeAdapter : IBadgeAdapter
{
    public string AppId => "Discord";
    public Task<int?> GetUnreadCountAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<int?>(OperatingSystem.IsWindows() ? 3 : null);
}

public sealed class WeChatBadgeAdapter : IBadgeAdapter
{
    public string AppId => "WeChat";
    public Task<int?> GetUnreadCountAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<int?>(OperatingSystem.IsWindows() ? 1 : null);
}

public sealed class BadgeAdapterRegistry
{
    private readonly IReadOnlyList<IBadgeAdapter> _adapters;

    public BadgeAdapterRegistry(IEnumerable<IBadgeAdapter>? adapters = null)
    {
        _adapters = adapters?.ToList() ?? new List<IBadgeAdapter>
        {
            new DiscordBadgeAdapter(),
            new WeChatBadgeAdapter(),
            new GenericBadgeAdapter("QQ", 2),
            new GenericBadgeAdapter("TIM", 0),
            new GenericBadgeAdapter("DingTalk", 5),
            new GenericBadgeAdapter("AliWangwang", 0),
            new GenericBadgeAdapter("YY", 0)
        };
    }

    public IReadOnlyList<IBadgeAdapter> Adapters => _adapters;

    public async Task<IReadOnlyDictionary<string, int?>> PollAllAsync(CancellationToken ct = default)
    {
        var result = new Dictionary<string, int?>(StringComparer.OrdinalIgnoreCase);
        foreach (var adapter in _adapters)
            result[adapter.AppId] = await adapter.GetUnreadCountAsync(ct).ConfigureAwait(false);
        return result;
    }
}

internal sealed class GenericBadgeAdapter : IBadgeAdapter
{
    private readonly int? _count;
    public GenericBadgeAdapter(string appId, int? count) { AppId = appId; _count = count; }
    public string AppId { get; }
    public Task<int?> GetUnreadCountAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_count);
}
