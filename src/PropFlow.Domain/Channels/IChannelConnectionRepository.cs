namespace PropFlow.Domain.Channels;

public interface IChannelConnectionRepository
{
    Task<ChannelConnection?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ChannelConnection>> FindActiveByPropertyAsync(Guid propertyId, CancellationToken ct = default);
    Task SaveAsync(ChannelConnection connection, CancellationToken ct = default);
    Task UpdateAsync(ChannelConnection connection, CancellationToken ct = default);
}
