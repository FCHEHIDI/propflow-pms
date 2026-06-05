namespace PropFlow.Domain.Channels;

/// <summary>
/// Resolves the correct IChannelAdapter by channel code.
/// Registered adapters: "booking.com", "expedia", "airbnb".
/// Throws if the channel code is unknown.
/// </summary>
public interface IChannelAdapterFactory
{
    IChannelAdapter GetAdapter(string channelCode);
}
