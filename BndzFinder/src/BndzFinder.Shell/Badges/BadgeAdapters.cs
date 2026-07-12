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
        Task.FromResult<int?>(null);
}

public sealed class WeChatBadgeAdapter : IBadgeAdapter
{
    public string AppId => "WeChat";
    public Task<int?> GetUnreadCountAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<int?>(null);
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
            new GenericBadgeAdapter("QQ"),
            new GenericBadgeAdapter("TIM"),
            new GenericBadgeAdapter("DingTalk"),
            new GenericBadgeAdapter("AliWangwang"),
            new GenericBadgeAdapter("YY")
        };
    }

    public IReadOnlyList<IBadgeAdapter> Adapters => _adapters;
}

internal sealed class GenericBadgeAdapter : IBadgeAdapter
{
    public GenericBadgeAdapter(string appId) => AppId = appId;
    public string AppId { get; }
    public Task<int?> GetUnreadCountAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<int?>(null);
}
