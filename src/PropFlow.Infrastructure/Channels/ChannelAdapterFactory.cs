using PropFlow.Domain.Channels;

namespace PropFlow.Infrastructure.Channels;

public sealed class ChannelAdapterFactory : IChannelAdapterFactory
{
    private readonly IReadOnlyDictionary<string, IChannelAdapter> _adapters;

    public ChannelAdapterFactory(IEnumerable<IChannelAdapter> adapters)
        => _adapters = adapters.ToDictionary(a => a.ChannelCode, StringComparer.OrdinalIgnoreCase);

    public IChannelAdapter GetAdapter(string channelCode)
    {
        if (_adapters.TryGetValue(channelCode, out var adapter))
            return adapter;

        throw new InvalidOperationException(
            $"No channel adapter registered for '{channelCode}'. " +
            $"Available: {string.Join(", ", _adapters.Keys)}");
    }
}
