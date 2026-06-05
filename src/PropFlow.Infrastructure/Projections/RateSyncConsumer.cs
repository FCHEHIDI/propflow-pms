using MassTransit;
using Marten;
using Microsoft.Extensions.Logging;
using PropFlow.Domain.Channels;
using PropFlow.Domain.Events;
using PropFlow.Domain.RatePlans;

namespace PropFlow.Infrastructure.Projections;

/// <summary>
/// Triggers ARI rate sync when a public RatePlan is published.
/// Pushes updated prices to all active OTA channel connections for the property.
///
/// ARI rate messages are distinct from ARI availability messages — OTAs require both.
/// </summary>
public sealed class RateSyncConsumer : IConsumer<RatePlanPublished>
{
    private readonly IDocumentSession _session;
    private readonly IChannelAdapterFactory _adapterFactory;
    private readonly ILogger<RateSyncConsumer> _logger;

    public RateSyncConsumer(
        IDocumentSession session,
        IChannelAdapterFactory adapterFactory,
        ILogger<RateSyncConsumer> logger)
    {
        _session        = session;
        _adapterFactory = adapterFactory;
        _logger         = logger;
    }

    public async Task Consume(ConsumeContext<RatePlanPublished> context)
    {
        var e  = context.Message;
        var ct = context.CancellationToken;
        if (!e.IsPublic) return;

        var ratePlan = await _session.LoadAsync<RatePlan>(e.RatePlanId, ct);
        if (ratePlan is null) return;

        var connections = await _session.Query<ChannelConnection>()
            .Where(c => c.PropertyId == e.PropertyId
                     && c.Status == ChannelConnectionStatus.Active)
            .ToListAsync(ct);

        foreach (var connection in connections)
        {
            var planMapping = connection.RatePlanMappings
                .FirstOrDefault(m => m.InternalRatePlanId == e.RatePlanId);
            if (planMapping is null) continue;

            try
            {
                var adapter = _adapterFactory.GetAdapter(connection.ChannelCode);

                foreach (var price in ratePlan.Prices)
                {
                    var rtMapping = connection.RoomTypeMappings
                        .FirstOrDefault(m => m.InternalRoomTypeId == price.RoomTypeId);
                    if (rtMapping is null) continue;

                    // Push rates for the next 365 days
                    for (var d = 0; d < 365; d++)
                    {
                        await adapter.PushRateAsync(
                            connection.Credentials,
                            new RateUpdate(
                                connection.Credentials.HotelId,
                                rtMapping.ExternalRoomTypeCode,
                                planMapping.ExternalRatePlanCode,
                                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(d)),
                                price.BaseRate,
                                price.ExtraAdultRate,
                                price.ExtraChildRate,
                                // TODO: fetch currency from Property aggregate
                                "EUR"),
                            ct);
                    }
                }

                connection.RecordSync(ChannelSyncStatus.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Rate sync failed for channel {ChannelCode} connection {ConnectionId}",
                    connection.ChannelCode, connection.Id);

                connection.RecordSync(ChannelSyncStatus.Failed);

                await context.Publish(
                    new ChannelSyncFailed(
                        connection.Id,
                        e.PropertyId,
                        connection.ChannelCode,
                        ex.Message),
                    ct);
            }

            _session.Store(connection);
        }

        await _session.SaveChangesAsync(ct);
    }
}
