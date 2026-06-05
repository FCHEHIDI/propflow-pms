using MassTransit;
using Marten;
using PropFlow.Domain.Events;
using PropFlow.Domain.Rooms;

namespace PropFlow.Infrastructure.Consumers;

/// <summary>
/// Responds to BookingCheckedOut by transitioning the Room to VacantDirty.
/// This starts the housekeeping signal chain:
///   VacantDirty → RoomStatusChanged → IoTPanelService + HousekeepingBoard
/// </summary>
public sealed class BookingCheckedOutConsumer : IConsumer<BookingCheckedOut>
{
    private readonly IDocumentSession _session;

    public BookingCheckedOutConsumer(IDocumentSession session) => _session = session;

    public async Task Consume(ConsumeContext<BookingCheckedOut> context)
    {
        var e = context.Message;
        var room = await _session.LoadAsync<Room>(e.RoomId, context.CancellationToken);
        if (room is null) return;

        room.Vacate();
        _session.Store(room);
        await _session.SaveChangesAsync(context.CancellationToken);
        room.ClearDomainEvents();
    }
}

/// <summary>
/// Responds to RoomRemovedFromInventory by calling Allotment.RemoveRoom()
/// for all future dates of this RoomType.
/// </summary>
public sealed class RoomRemovedFromInventoryConsumer : IConsumer<RoomRemovedFromInventory>
{
    private readonly IDocumentSession _session;

    public RoomRemovedFromInventoryConsumer(IDocumentSession session) => _session = session;

    public async Task Consume(ConsumeContext<RoomRemovedFromInventory> context)
    {
        var e = context.Message;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Update allotments for the next 365 days
        var allotments = await _session.Query<Domain.Inventory.Allotment>()
            .Where(a => a.PropertyId == e.PropertyId
                && a.RoomTypeId == e.RoomTypeId
                && a.Date >= today)
            .ToListAsync(context.CancellationToken);

        foreach (var allotment in allotments)
        {
            allotment.RemoveRoom();
            _session.Store(allotment);
        }

        await _session.SaveChangesAsync(context.CancellationToken);
    }
}

/// <summary>
/// Forwards InventoryUpdated events to all active ChannelConnections for the property.
/// Each channel adapter pushes the ARI update to its OTA.
/// </summary>
public sealed class InventoryUpdatedConsumer : IConsumer<InventoryUpdated>
{
    // TODO: inject IChannelConnectionRepository + channel adapters
    public Task Consume(ConsumeContext<InventoryUpdated> context)
    {
        // TODO: foreach active channel connection for PropertyId:
        //   adapter.PushAvailability(RoomTypeMapping, Date, Available)
        return Task.CompletedTask;
    }
}
