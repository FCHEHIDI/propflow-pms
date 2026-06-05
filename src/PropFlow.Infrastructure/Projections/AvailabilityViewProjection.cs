using MassTransit;
using Marten;
using PropFlow.Application.ReadModels;
using PropFlow.Domain.Events;

namespace PropFlow.Infrastructure.Projections;

/// <summary>
/// Maintains the AvailabilityView read model.
/// One document per (PropertyId × RoomTypeId × Date), upserted on every InventoryUpdated event.
/// This is what the channel manager queries before pushing ARI updates to OTAs.
/// </summary>
public sealed class AvailabilityViewProjection : IConsumer<InventoryUpdated>
{
    private readonly IDocumentSession _session;

    public AvailabilityViewProjection(IDocumentSession session) => _session = session;

    public async Task Consume(ConsumeContext<InventoryUpdated> context)
    {
        var e = context.Message;
        var id = AvailabilityView.ComputeId(e.PropertyId, e.RoomTypeId, e.Date);

        var view = await _session.LoadAsync<AvailabilityView>(id, context.CancellationToken)
            ?? new AvailabilityView
            {
                Id = id,
                PropertyId = e.PropertyId,
                RoomTypeId = e.RoomTypeId,
                Date = e.Date,
            };

        view.Available = e.Available;
        view.LastUpdatedAt = e.OccurredAt;

        _session.Store(view);
        await _session.SaveChangesAsync(context.CancellationToken);
    }
}
