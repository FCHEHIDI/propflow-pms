using MassTransit;
using Marten;
using PropFlow.Domain.Channels;
using PropFlow.Domain.Events;

namespace PropFlow.Infrastructure.Consumers;

/// <summary>
/// Replaces the TODO stub in DomainEventConsumers.cs.
/// For every InventoryUpdated event, finds all active ChannelConnections for the property
/// and pushes the ARI availability update via the correct adapter.
/// </summary>
public sealed class InventoryUpdatedConsumer : IConsumer<InventoryUpdated>
{
    private readonly IDocumentSession _session;
    private readonly IChannelAdapterFactory _adapterFactory;

    public InventoryUpdatedConsumer(
        IDocumentSession session,
        IChannelAdapterFactory adapterFactory)
    {
        _session = session;
        _adapterFactory = adapterFactory;
    }

    public async Task Consume(ConsumeContext<InventoryUpdated> context)
    {
        var e = context.Message;

        var connections = await _session.Query<ChannelConnection>()
            .Where(c => c.PropertyId == e.PropertyId
                && c.Status == ChannelConnectionStatus.Active)
            .ToListAsync(context.CancellationToken);

        foreach (var connection in connections)
        {
            var mapping = connection.RoomTypeMappings
                .FirstOrDefault(m => m.InternalRoomTypeId == e.RoomTypeId);

            if (mapping is null) continue;

            try
            {
                var adapter = _adapterFactory.GetAdapter(connection.ChannelCode);
                await adapter.PushAvailabilityAsync(
                    connection.Credentials,
                    new AvailabilityUpdate(
                        connection.Credentials.HotelId,
                        mapping.ExternalRoomTypeCode,
                        e.Date,
                        e.Available),
                    context.CancellationToken);

                connection.RecordSync(ChannelSyncStatus.Success);
            }
            catch (Exception ex)
            {
                connection.RecordSync(ChannelSyncStatus.Failed);

                await context.Publish(
                    new ChannelSyncFailed(
                        connection.Id,
                        e.PropertyId,
                        connection.ChannelCode,
                        ex.Message),
                    context.CancellationToken);
            }

            _session.Store(connection);
        }

        await _session.SaveChangesAsync(context.CancellationToken);
    }
}
