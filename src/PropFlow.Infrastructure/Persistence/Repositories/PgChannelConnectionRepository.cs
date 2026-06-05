using MassTransit;
using Marten;
using PropFlow.Domain.Channels;

namespace PropFlow.Infrastructure.Persistence.Repositories;

public sealed class PgChannelConnectionRepository : IChannelConnectionRepository
{
    private readonly IDocumentSession _session;
    private readonly IPublishEndpoint _bus;

    public PgChannelConnectionRepository(IDocumentSession session, IPublishEndpoint bus)
    {
        _session = session;
        _bus     = bus;
    }

    public async Task<ChannelConnection?> GetAsync(Guid id, CancellationToken ct = default)
        => await _session.LoadAsync<ChannelConnection>(id, ct);

    public async Task<IReadOnlyList<ChannelConnection>> GetActiveForPropertyAsync(
        Guid propertyId, CancellationToken ct = default)
        => await _session.Query<ChannelConnection>()
            .Where(c => c.PropertyId == propertyId
                     && c.Status == ChannelConnectionStatus.Active)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ChannelConnection>> GetAllForPropertyAsync(
        Guid propertyId, CancellationToken ct = default)
        => await _session.Query<ChannelConnection>()
            .Where(c => c.PropertyId == propertyId)
            .ToListAsync(ct);

    public async Task SaveAsync(ChannelConnection connection, CancellationToken ct = default)
    {
        _session.Store(connection);
        await _session.SaveChangesAsync(ct);
        foreach (var e in connection.DomainEvents) await _bus.Publish(e, ct);
        connection.ClearDomainEvents();
    }
}
