using MassTransit;
using Marten;
using PropFlow.Domain.Channels;
using PropFlow.Domain.Events;

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

    public RateSyncConsumer(IDocumentSession session) => _session = session;

    public async Task Consume(ConsumeContext<RatePlanPublished> context)
    {
        var e = context.Message;
        if (!e.IsPublic) return;

        var connections = await _session.Query<ChannelConnection>()
            .Where(c => c.PropertyId == e.PropertyId
                && c.Status == ChannelConnectionStatus.Active)
            .ToListAsync(context.CancellationToken);

        foreach (var connection in connections)
        {
            var mapping = connection.RatePlanMappings
                .FirstOrDefault(m => m.InternalRatePlanId == e.RatePlanId);

            if (mapping is null) continue;

            // TODO: inject IChannelAdapterFactory
            // var adapter = adapterFactory.GetAdapter(connection.ChannelCode);
            // await adapter.PushRatesAsync(connection.Credentials, mapping.ExternalRatePlanCode, prices, ct);
        }
    }
}
